Image and MP4 Web Builder

Overview
- A WPF (.NET 8) desktop tool for browsing a folder of images (.png, .jpg, .jpeg) and videos (.mp4), generating thumbnails, and exporting a simple, multi-page HTML gallery.
- Thumbnails are created into a "thumbnails" subfolder next to your media.

Basic Usage
1) Launch the app and click "Select Folder" to choose a directory containing images and/or MP4 videos.
   - Thumbnails are generated as needed and saved in the "thumbnails" subfolder.
2) Browse media with paging (up to 40 items per page). Use "< Prev" and "Next >" to navigate.
3) Open an item by clicking its thumbnail:
   - Images: open in a simple preview window inside the app.
   - Videos: open with your system's default player.
4) Export an HTML gallery by clicking "Export HTML":
   - You will be prompted for a base page name (pre-filled with the selected folder name).
   - If the "Ignore NSFW" checkbox is unchecked, "_NSFW" will be appended to the base name.
   - The app generates one or more pages (Page 1, Page 2, ...) depending on item count.
   - Each page contains responsive thumbnail tiles. Clicking an image opens a fullscreen popup; click again or press Escape to return.

Keyboard
- Ctrl+H: Open the in-app Help window.

Back Link (Return/Back button)
- If the selected folder contains a file named "out.txt" (or "out .txt" with a space) with a URL on the first non-empty line, the first exported page will show a Back button linking to that URL.
- If the selected folder name itself contains "NSFW" and the back URL ends with ".html" but not "_NSFW.html", the link is adjusted to "_NSFW.html".

Subfolder Links
- On export, if any immediate subfolder contains an HTML file with the same name as that subfolder (e.g., Photos/Photos.html), the exported pages will include links to those subfolder pages.
- If the "Ignore NSFW" box is checked, subfolders whose names contain "NSFW" will be skipped from the list.
- After export, for each immediate subfolder that does NOT contain its own subfolder-named HTML, an "out.txt" file is written to that subfolder with a relative link pointing back to the main exported page. This helps sub-pages link up to the parent index.

Empty Folders
- Export still works even if no media are found. The page will render a friendly message and any applicable subfolder links.

File Naming and Pagination
- Base name prompt defaults to the selected folder name; "_NSFW" is appended if the ignore box is unchecked.
- Page files are named: Base.html (first page), Base2.html, Base3.html, ...
- Navigation labels show "Page 1", "Page 2", etc.
- Default page size is 40 items per page.

Formats Supported
- Images: .png, .jpg, .jpeg
- Videos: .mp4

Generated HTML Details
- Responsive grid layout for thumbnails, with hover effects for a cleaner look.
- Video thumbnails include a small ? badge overlay.
- Images open in a fullscreen overlay (lightbox). Click the overlay or press Escape to close.
- Subfolder links are displayed as small chips under the header area.

Troubleshooting
- Thumbnails not generating for videos: Ensure the app can access libVLC native binaries provided by LibVLCSharp (via NuGet). If issues persist, try installing VLC and confirm media files are readable.
- Permission errors: Ensure the selected folder is writable for creating the "thumbnails" folder and writing exported HTML files.
- Relative links: Exported pages use relative paths. Keep the HTML pages and the media folder structure intact when moving or uploading.

Notes
- The app is a desktop utility intended for local folder browsing and static HTML generation. It does not upload files.
- HTML pages are plain, static files that can be opened locally or hosted on a simple web server.

License
Copyright(c) 2019-2025 by Saul Scudder

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and /or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright noticeand this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

- See repository for license and third-party attribution (LibVLCSharp).
