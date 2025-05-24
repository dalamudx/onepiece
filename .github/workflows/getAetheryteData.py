#!/usr/bin/env python
# -*- coding: utf-8 -*-

import json
import os
import requests
from bs4 import BeautifulSoup
import time
import re
import logging
import argparse

try:
    from rich.console import Console
    from rich.table import Table
    from rich.panel import Panel
    from rich.progress import Progress, SpinnerColumn, BarColumn, TextColumn
    from rich.logging import RichHandler
    from rich import print as rprint
    RICH_AVAILABLE = True
except ImportError:
    import subprocess
    import sys
    
    print("Installing rich library for better output display...")
    try:
        subprocess.check_call([sys.executable, "-m", "pip", "install", "rich"])
        from rich.console import Console
        from rich.table import Table
        from rich.panel import Panel
        from rich.progress import Progress, SpinnerColumn, BarColumn, TextColumn
        from rich.logging import RichHandler
        from rich import print as rprint
        print("Successfully installed rich library!")
        RICH_AVAILABLE = True
    except Exception as e:
        print(f"Failed to install rich library: {e}\nWill use standard output")
        RICH_AVAILABLE = False

# Initialize Rich console
console = Console() if RICH_AVAILABLE else None
progress = None

# Initialize logging system
def setup_logger(log_level_name):
    """Initialize logging system with the specified log level
    
    Args:
        log_level_name (str): Log level name (debug, info, warning, error, critical, none)
    
    Returns:
        Logger: Configured logger instance
    """
    # Map log level names to logging module levels
    log_levels = {
        'debug': logging.DEBUG,
        'info': logging.INFO,
        'warning': logging.WARNING,
        'error': logging.ERROR,
        'critical': logging.CRITICAL,
        'none': logging.CRITICAL + 10  # Level higher than CRITICAL to disable all logging
    }
    
    # Get the numeric log level (default to CRITICAL+10 if not found)
    log_level = log_levels.get(log_level_name.lower(), logging.CRITICAL + 10)
    
    # Configure the logging system
    logging.basicConfig(
        level=log_level,
        format="%(message)s",
        handlers=[RichHandler(rich_tracebacks=True)] if RICH_AVAILABLE else None
    )
    
    logger = logging.getLogger('aetheryte_data')
    logger.setLevel(log_level)
    
    return logger

# Global variable to store processed map areas to avoid duplicate processing
processed_areas = set()

# Request headers to simulate browser access
headers = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3'
}

# Cache directory
cache_dir = "wiki_cache"

# Global logger object
logger = None

# String similarity function for name matching
def similarity(s1, s2, map_area=None):
    """Calculate the similarity between two strings, enhanced for main city aetheryte matching"""
    # Simple similarity calculation, convert to lowercase and remove special characters
    s1_clean = re.sub(r'[^\w\s]', '', s1.lower())
    s2_clean = re.sub(r'[^\w\s]', '', s2.lower())
    
    # Exact match check - directly return true for exact matches
    if s1_clean == s2_clean:
        return True
    
    # If one is a substring of the other, consider them similar
    # Special case: main city aetheryte matching
    if map_area and ("limsa lominsa" in map_area.lower() or 
                    "gridania" in map_area.lower() or 
                    "ul'dah" in map_area.lower() or
                    "ishgard" in map_area.lower() or
                    "kugane" in map_area.lower() or
                    "crystarium" in map_area.lower() or
                    "old sharlayan" in map_area.lower()):
        # Main city aetheryte names may have significant differences, use more relaxed matching
        if "aetheryte" in s1_clean and "plaza" in s1_clean and map_area.lower().replace("'", "") in s1_clean:
            return True
        if "aetheryte" in s2_clean and "plaza" in s2_clean and map_area.lower().replace("'", "") in s2_clean:
            return True
    
    # If the string length difference is too large, consider them dissimilar
    length_diff = abs(len(s1_clean) - len(s2_clean))
    if length_diff > min(len(s1_clean), len(s2_clean)) / 3:
        return False
    
    # More strict character comparison
    # Count consecutive matching characters (better for word-by-word matching)
    s1_words = s1_clean.split()
    s2_words = s2_clean.split()
    
    # If the number of words is significantly different, they're likely different locations
    if abs(len(s1_words) - len(s2_words)) > 1:
        return False
    
    # Check if at least one word exactly matches between the strings
    matching_words = sum(1 for w1 in s1_words if any(w1 == w2 for w2 in s2_words))
    if matching_words == 0:
        return False
    
    # Calculate the similarity ratio using character matching as a backup
    common = sum(1 for c in s1_clean if c in s2_clean)
    total = max(len(s1_clean), len(s2_clean))
    char_similarity = common / total
    
    # Require at least 85% character similarity for non-exact matches
    # This is more strict than the previous 80% threshold
    return char_similarity > 0.85

def get_wiki_data(map_area):
    """Retrieve aetheryte coordinates for a specified map area from the FFXIV Wiki"""
    # Replace underscores with spaces for URL
    url_map_area = map_area.replace('_', ' ')
    
    # Handle special cases: encoding of spaces and special characters in URLs
    url_map_area = url_map_area.replace(' ', '_')
    
    # Build Wiki URL
    url = f"https://ffxiv.consolegameswiki.com/wiki/{url_map_area}"
    logger.debug(f"Trying to access URL: {url}")
    
    # Create cache directory
    if not os.path.exists(cache_dir):
        os.makedirs(cache_dir)
    
    # Cache file path
    cache_file = os.path.join(cache_dir, f"{map_area}.html")
    
    # Option to force refresh cache
    force_refresh = False  # Set to True to force refresh, change back to False after debugging
    
    # If cache exists and is not too old, use it directly
    if not force_refresh and os.path.exists(cache_file) and (time.time() - os.path.getmtime(cache_file)) < 86400:  # 24 hour cache
        with open(cache_file, 'r', encoding='utf-8') as f:
            html_content = f.read()
        logger.debug(f"Loaded from cache: {cache_file}")
    else:
        try:
            logger.info(f"Directly accessing Wiki page: {url}")
            response = requests.get(url, headers=headers, timeout=10)
            response.raise_for_status()  # Check if the request was successful
            
            html_content = response.text
            
            # Save to cache
            with open(cache_file, 'w', encoding='utf-8') as f:
                f.write(html_content)
            logger.debug(f"Saved webpage to cache: {cache_file}")
                
            # Add delay between requests to avoid putting too much pressure on the server
            time.sleep(1)
            
        except requests.RequestException as e:
            logger.error(f"Request error: {e}")
            return []
    
    # Use BeautifulSoup to parse HTML
    soup = BeautifulSoup(html_content, 'html.parser')
    
    # Debug: Check page title
    title = soup.find('title')
    if title:
        logger.info(f"Page title: {title.text}")
    
    # Find DL elements containing Aetherytes information
    aetheryte_data = []
    dl_elements = soup.find_all('dl')
    
    logger.debug(f"Found {len(dl_elements)} dl elements in the page")
    
    # First, try to find using the original method
    for dl in dl_elements:
        dt_elements = dl.find_all('dt')
        for dt in dt_elements:
            dt_text = dt.get_text().strip()
            logger.debug(f"Checking dt element: '{dt_text}'")
            if dt_text == 'Aetherytes':
                # Find the corresponding dd element containing teleport point information
                dd = dt.find_next('dd')
                if dd:
                    # Parse each teleport point and coordinates in dd
                    text = dd.get_text()
                    logger.debug(f"Found dd element content for Aetherytes: {text}")
                    
                    # Merged regular expression pattern - supports all formats
                    patterns = [
                        # Main pattern: supports both "X:31, Y:30" and "(10.5, 20.8)" formats, integers and decimals
                        r'([^(]+)\s*\(\s*(?:X:\s*)?(\d+(?:\.\d+)?)\s*(?:,|\s)\s*(?:Y:\s*)?(\d+(?:\.\d+)?)\s*\)'
                    ]
                    
                    # Try all patterns
                    matches = []
                    for pattern in patterns:
                        pattern_matches = re.findall(pattern, text)
                        if pattern_matches:
                            matches.extend(pattern_matches)
                            logger.debug(f"Found {len(pattern_matches)} matches using pattern '{pattern}'")
                    
                    if not matches:
                        logger.info("No coordinate data could be matched with any pattern")
                    
                    for match in matches:
                        name = match[0].strip()
                        x = float(match[1])
                        y = float(match[2])
                        aetheryte_data.append({
                            'Name': name,
                            'X': x,
                            'Y': y
                        })
                        logger.debug(f"Extracted teleport point: {name} (X: {x}, Y: {y})")
    
    # If the original method did not find data, try using other methods
    if not aetheryte_data:
        print("Original method did not find data, trying fallback methods...")
        
        # Try to directly find elements marked as Aetherytes
        for element in soup.find_all(string=re.compile('Aetherytes')):
            logger.debug(f"Found element containing Aetherytes text: {element}")
            parent = element.parent
            sibling = parent.find_next_sibling()
            if sibling:
                text = sibling.get_text()
                logger.debug(f"Adjacent element content: {text}")
                
                # Try various regular expression matching patterns
                for pattern in patterns:
                    pattern_matches = re.findall(pattern, text)
                    if pattern_matches:
                        for match in pattern_matches:
                            name = match[0].strip()
                            x = float(match[1])
                            y = float(match[2])
                            aetheryte_data.append({
                                'Name': name,
                                'X': x,
                                'Y': y
                            })
                            logger.debug(f"Extracted teleport point using fallback method: {name} (X: {x}, Y: {y})")
        
        # If still no data is found, try searching the entire page for coordinate formats
        if not aetheryte_data:
            print("Trying the last method: searching the entire page for coordinate formats...")
            # Extract the entire page's text
            page_text = soup.get_text()
            
            # Try to match all possible coordinate formats
            for pattern in patterns:
                page_matches = re.findall(pattern, page_text)
                if page_matches:
                    logger.debug(f"Found {len(page_matches)} matches using pattern '{pattern}' in the entire page")
                    # Filter possible teleport point coordinates (remove matches that are clearly not teleport points)
                    for match in page_matches:
                        name = match[0].strip()
                        # Check if it contains possible teleport point keywords
                        if any(keyword in name.lower() for keyword in ['aetheryte', 'camp', 'settlement', 'outpost', 'city', 'town']):
                            x = float(match[1])
                            y = float(match[2])
                            aetheryte_data.append({
                                'Name': name,
                                'X': x,
                                'Y': y
                            })
                            logger.debug(f"Extracted possible teleport point: {name} (X: {x}, Y: {y})")
    
    # Remove duplicates
    unique_data = []
    seen_names = set()
    for item in aetheryte_data:
        if item['Name'] not in seen_names:
            seen_names.add(item['Name'])
            unique_data.append(item)
    
    logger.info(f"Found a total of {len(unique_data)} unique teleport points")
    return unique_data

def update_json_with_coords(json_file_path):
    """Update the aetheryte coordinates in the JSON file"""
    try:
        # Check if the file exists
        if not os.path.exists(json_file_path):
            logger.error(f"Error: File {json_file_path} does not exist!")
            return
        
        # Read the JSON file
        with open(json_file_path, 'r', encoding='utf-8') as file:
            data = json.load(file)
        
        # Check the data structure
        if 'Aetherytes' not in data:
            logger.error("Error: 'Aetherytes' field not found in the JSON file!")
            return
        
        # Collect all unique MapAreas
        map_areas = {}
        for aetheryte in data['Aetherytes']:
            if 'MapArea' in aetheryte and aetheryte['MapArea']:
                map_area = aetheryte['MapArea']
                map_area_key = map_area.replace(' ', '_')
                map_areas[map_area_key] = map_area
        
        # Create cache directory
        if not os.path.exists(cache_dir):
            os.makedirs(cache_dir)
        
        # Process each map area
        updated_count = 0
        total_skipped = 0  # Total number of skipped aetherytes
        skipped_areas = 0  # Number of skipped areas
        
        # Define function to process a single map area
        def process_map_area(map_area_key, map_area, data, json_file_path):
            nonlocal updated_count, total_skipped, skipped_areas
            
            if map_area_key in processed_areas:
                # Skip already processed areas without showing notification to avoid redundant output
                return
            
            # Check if all aetherytes in this area already have coordinates
            aetherytes_in_area = [a for a in data['Aetherytes'] if a.get('MapArea') == map_area]
            need_coordinates = [a for a in aetherytes_in_area if a['X'] == 0 and a['Y'] == 0]
            
            if not need_coordinates:
                # Skip area without notification to avoid redundant output
                skipped_areas += 1
                return
                
            # 从console wiki获取坐标数据
            wiki_data = get_wiki_data(map_area_key)
            
            if not wiki_data:
                if RICH_AVAILABLE:
                    console.print(f"[yellow]未找到 {map_area} 的数据，跳过[/]")
                else:
                    print(f"未找到 {map_area} 的数据，跳过")
                return
            
            if RICH_AVAILABLE:
                console.print(f"[green]Found {len(wiki_data)} teleport points[/]")
            else:
                print(f"Found {len(wiki_data)} teleport points:")
            
            # Match and update coordinates
            area_updated = False
            skipped_count = 0  # Number of skipped items in this area
            
            # First find teleport points with Aetheryte Plaza
            plaza_matches = [a for a in wiki_data if "aetheryte plaza" in a['Name'].lower() or "aetherite plaza" in a['Name'].lower()]
            
            for aeth_json in data['Aetherytes']:
                if aeth_json.get('MapArea') != map_area:
                    continue
                
                # Then check if coordinates data already exists
                if aeth_json['X'] != 0 or aeth_json['Y'] != 0:
                    logger.debug(f"Skipping item with existing coordinates: {aeth_json['Name']} (X: {aeth_json['X']}, Y: {aeth_json['Y']})")
                    skipped_count += 1
                    total_skipped += 1
                    continue  # Skip items with existing coordinates
                
                # Check for special case: name related to the area and has Aetheryte Plaza
                if aeth_json['Name'] == aeth_json['MapArea'] and len(plaza_matches) == 1:
                    # This is an item that exactly matches the area name, and the wiki has one plaza teleport point
                    plaza_data = plaza_matches[0]
                    logger.debug(f"Special case - Area name match: {aeth_json['Name']} -> {plaza_data['Name']} (X: {plaza_data['X']}, Y: {plaza_data['Y']})")
                    aeth_json['X'] = plaza_data['X']
                    aeth_json['Y'] = plaza_data['Y']
                    area_updated = True
                    updated_count += 1
                    continue
                
                # Regular matching
                matched = False
                for aeth_data in wiki_data:
                    if similarity(aeth_json['Name'], aeth_data['Name'], map_area):
                        logger.debug(f"Updating coordinates: {aeth_json['Name']} -> X: {aeth_data['X']}, Y: {aeth_data['Y']}")
                        aeth_json['X'] = aeth_data['X']
                        aeth_json['Y'] = aeth_data['Y']
                        area_updated = True
                        updated_count += 1
                        matched = True
                        break
            
            # 显示跳过的条目数量
            if skipped_count > 0:
                if RICH_AVAILABLE:
                    console.print(f"[yellow]Skipped {skipped_count} items with existing coordinates[/]")
                else:
                    print(f"Skipped {skipped_count} items with existing coordinates")
            
            if area_updated:
                # Write the updated JSON
                with open(json_file_path, 'w', encoding='utf-8') as file:
                    json.dump(data, file, indent=2)
                if RICH_AVAILABLE:
                    console.print(f"[green]Updates saved - {map_area}[/]")
                else:
                    print(f"Updates saved - {map_area}")
            
            # Mark as processed
            processed_areas.add(map_area_key)
        
        # Prepare to collect processing results
        results = {}
        
        # Redefine process_map_area function to return results instead of directly outputting
        def process_map_area_silent(map_area_key, map_area, data, json_file_path):
            nonlocal updated_count, total_skipped, skipped_areas
            result = {"updated": False, "skipped": 0, "message": []}
            
            if map_area_key in processed_areas:
                return result
            
            # Check if all teleport points in this area already have coordinates
            aetherytes_in_area = [a for a in data['Aetherytes'] if a.get('MapArea') == map_area]
            need_coordinates = [a for a in aetherytes_in_area if a['X'] == 0 and a['Y'] == 0]
            
            if not need_coordinates:
                skipped_areas += 1
                return result
                
            # Get coordinate data from console wiki
            wiki_data = get_wiki_data(map_area_key)
            
            if not wiki_data:
                result["message"].append(f"No data found for {map_area}, skipping")
                return result
            
            result["message"].append(f"Found {len(wiki_data)} teleport points")
            
            # Match and update coordinates
            area_updated = False
            skipped_count = 0  # Number of skipped items in this area
            
            # First find teleport points with Aetheryte Plaza
            plaza_matches = [a for a in wiki_data if "aetheryte plaza" in a['Name'].lower() or "aetherite plaza" in a['Name'].lower()]
            
            for aeth_json in data['Aetherytes']:
                if aeth_json.get('MapArea') != map_area:
                    continue
                
                # Then check if coordinates data already exists
                if aeth_json['X'] != 0 or aeth_json['Y'] != 0:
                    skipped_count += 1
                    total_skipped += 1
                    continue  # Skip items with existing coordinates
                
                # Check for special case: name related to the area and has Aetheryte Plaza
                if aeth_json['Name'] == aeth_json['MapArea'] and len(plaza_matches) == 1:
                    # This is an item that exactly matches the area name, and the wiki has one plaza teleport point
                    plaza_data = plaza_matches[0]
                    aeth_json['X'] = plaza_data['X']
                    aeth_json['Y'] = plaza_data['Y']
                    area_updated = True
                    updated_count += 1
                    continue
                
                # Regular matching
                matched = False
                for aeth_data in wiki_data:
                    if similarity(aeth_json['Name'], aeth_data['Name'], map_area):
                        aeth_json['X'] = aeth_data['X']
                        aeth_json['Y'] = aeth_data['Y']
                        area_updated = True
                        updated_count += 1
                        matched = True
                        break
            
            # Display the number of skipped items
            if skipped_count > 0:
                result["message"].append(f"Skipped {skipped_count} items with existing coordinates")
            
            result["updated"] = area_updated
            result["skipped"] = skipped_count
            
            if area_updated:
                # Write the updated JSON
                with open(json_file_path, 'w', encoding='utf-8') as file:
                    json.dump(data, file, indent=2)
                result["message"].append(f"Updates saved - {map_area}")
            
            # Mark as processed
            processed_areas.add(map_area_key)
            return result
        
        # Use Rich progress bar
        if RICH_AVAILABLE:
            console.print(Panel.fit("[bold blue]Starting to process map areas...[/]", border_style="green"))
            
            # First process all map areas silently and collect results
            with Progress(
                SpinnerColumn(),
                TextColumn("[bold blue]{task.description}[/]"),
                BarColumn(bar_width=40),
                TextColumn("[bold green]{task.completed}/{task.total}[/]"),
                expand=True
            ) as progress:
                task_id = progress.add_task("[cyan]Processing map areas...", total=len(map_areas))
                
                # Code to iterate through map areas - version with progress bar
                for map_area_key, map_area in sorted(map_areas.items()):
                    progress.update(task_id, description=f"[cyan]Processing map area: {map_area}")
                    results[map_area] = process_map_area_silent(map_area_key, map_area, data, json_file_path)
                    progress.update(task_id, advance=1)
            
            # Display results after progress bar completes
            console.print("\n[bold green]Processing results:[/]")
            for map_area, result in results.items():
                if result["message"]:
                    console.print(f"[cyan]{map_area}:[/]")
                    for msg in result["message"]:
                        console.print(f"  [yellow]{msg}[/]")
                    console.print("")
        else:
            # Regular processing - version without progress bar
            print("\nStarting to process map areas...")
            for map_area_key, map_area in sorted(map_areas.items()):
                print(f"\nProcessing map area: {map_area}")
                process_map_area(map_area_key, map_area, data, json_file_path)
        
        # Use Rich to beautify statistics output
        if RICH_AVAILABLE:
            stats_table = Table(show_header=False, box=None)
            stats_table.add_column("Metric", style="bold")
            stats_table.add_column("Value", style="cyan")
            
            stats_table.add_row("Updated teleport point coordinates", f"[green]{updated_count}[/]")
            stats_table.add_row("Skipped items with existing coordinates", f"[yellow]{total_skipped}[/]")
            stats_table.add_row("Skipped areas with complete coordinates", f"[yellow]{skipped_areas}[/]")
            
            console.print(Panel.fit(stats_table, title="[bold]Complete![/]", border_style="green"))
        else:
            logger.info(f"\nComplete! Updated {updated_count} teleport point coordinates")
            logger.info(f"Skipped {total_skipped} items with existing coordinates")
            logger.info(f"Skipped {skipped_areas} areas with complete coordinates")
        
    except json.JSONDecodeError:
        if RICH_AVAILABLE:
            console.print(f"[bold red]Error:[/] {json_file_path} is not a valid JSON file!")
        else:
            print(f"Error: {json_file_path} is not a valid JSON file!")
    except Exception as e:
        if logger:
            logger.info(f"Error occurred during processing: {str(e)}")
        import traceback
        traceback.print_exc()

def print_map_areas(json_file_path):
    """Print a list of all map areas"""
    try:
        # Check if the file exists
        if not os.path.exists(json_file_path):
            if RICH_AVAILABLE:
                console.print(f"[bold red]Error:[/] File does not exist: {json_file_path}")
            else:
                print(f"Error: File {json_file_path} does not exist!")
            return
        
        # Read the JSON file
        with open(json_file_path, 'r', encoding='utf-8') as file:
            data = json.load(file)
        
        # Check the data structure
        if 'Aetherytes' not in data:
            if RICH_AVAILABLE:
                console.print(f"[bold red]Error:[/] 'Aetherytes' field not found in the JSON file!")
            else:
                print(f"Error: 'Aetherytes' field not found in the JSON file!")
            return
        
        # Collect all unique MapAreas
        map_areas = set()
        for aetheryte in data['Aetherytes']:
            if 'MapArea' in aetheryte and aetheryte['MapArea']:
                map_areas.add(aetheryte['MapArea'])
        
        # Sort the map areas alphabetically
        sorted_map_areas = sorted(map_areas)
        
        # Display the MapArea list with Rich formatting
        if RICH_AVAILABLE:
            table = Table(title="Map Areas List", show_header=True, header_style="bold magenta")
            table.add_column("#", style="dim", width=6)
            table.add_column("MapArea", style="green")
            
            for i, map_area in enumerate(sorted_map_areas, 1):
                table.add_row(f"{i}", map_area)
            
            console.print(table)
            console.print(Panel.fit(f"Found [bold cyan]{len(sorted_map_areas)}[/] unique map areas", border_style="green"))
        else:
            print("MapArea names:")
            for i, map_area in enumerate(sorted_map_areas, 1):
                print(f"  {i}. {map_area}")
            
            print(f"Found {len(sorted_map_areas)} unique MapAreas")
        
    except json.JSONDecodeError:
        if RICH_AVAILABLE:
            console.print(f"[bold red]Error:[/] {json_file_path} is not a valid JSON file!")
        else:
            print(f"Error: {json_file_path} is not a valid JSON file!")
    except Exception as e:
        if RICH_AVAILABLE:
            console.print(f"[bold red]Error occurred during processing:[/] {str(e)}")
        else:
            print(f"Error occurred during processing: {str(e)}")
        import traceback
        traceback.print_exc()

def parse_arguments():
    """Parse command line arguments"""
    parser = argparse.ArgumentParser(description='FFXIV Aetheryte Data Tool')
    parser.add_argument('--json', '-j', default=r"aetheryte.json",
                      help='JSON file path')
    parser.add_argument('--loglevel', '-l', default='none',
                      choices=['debug', 'info', 'warning', 'error', 'critical', 'none'],
                      help='Log level (default: none)')
    return parser.parse_args()

if __name__ == "__main__":
    try:
        # Parse command line arguments
        args = parse_arguments()
        
        # Set up logger with log level from command line arguments
        logger = setup_logger(args.loglevel)
        
        # Use JSON file path specified in arguments
        json_file_path = args.json
        
        if RICH_AVAILABLE:
            console.print(f"[cyan]Using JSON file:[/] [yellow]{json_file_path}[/]")
        else:
            print(f"Using JSON file: {json_file_path}")
        
        if not os.path.exists(json_file_path):
            if RICH_AVAILABLE:
                console.print(f"[bold red]Error:[/] File does not exist: {json_file_path}")
            else:
                print(f"Error: File does not exist: {json_file_path}")
            exit(1)
        
        print_map_areas(json_file_path)
        
        if RICH_AVAILABLE:
            console.print(Panel.fit("[bold blue]Starting coordinate data update[/]", border_style="cyan"))
        else:
            print("\n==== Starting coordinate data update ====\n")
        update_json_with_coords(json_file_path)
        
    except Exception as e:
        if logger:
            logger.error(f"Main program error: {str(e)}")
        else:
            print(f"Main program error: {str(e)}")
        import traceback
        traceback.print_exc()
