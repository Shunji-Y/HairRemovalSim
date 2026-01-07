#!/bin/bash

# Check if ImageMagick is installed
if ! command -v magick &> /dev/null; then
    echo "ImageMagick is not installed. Please install it before running this script."
    exit 1
fi

# Function to process a single image
process_image() {
    local file="$1"
    echo "Processing $file"
    magick "$file" -write MPR:orig -channel B -separate -write "$1"newR.png    \
    \( MPR:orig -channel R -separate                    -write "$1"newG.png \) \
    \( MPR:orig -channel R -separate -threshold 100%    -write "$1"newB.png \) \
    \( MPR:orig -channel G -separate -negate            -write "$1"newA.png \) \
    -combine PNG32:"$file"

   rm "$1"newR.png "$1"newG.png "$1"newB.png "$1"newA.png
}

# Set the maximum number of concurrent processes
# You can adjust this number based on the number of CPU cores you have.
max_concurrent_processes=4

# Find all the files with the name containing 'Masks'
masks=( *Masks*.png )

# Loop through the files and process them in parallel
for ((i = 0; i < ${#masks[@]}; i += max_concurrent_processes)); do
    for ((j = 0; j < max_concurrent_processes && i + j < ${#masks[@]}; j++)); do
        process_image "${masks[i + j]}" &
    done
    wait
done

echo "Channel switching completed!"


 for i in *Normal*.png; do 
    echo "$i"
    magick convert "$i" -channel G -negate "$i"
 done