1. The texts given for a model version are HTML-encoded and may contain tags like `<br>`, `<b>`, `<i>`, etc. The frontend should decode these and render them appropriately, preserving line breaks and formatting.	
2. If there is no db file, a folder selection dialog should open prompting the user to select a directory to create the db in. The db file should be created as `cai.db` in the selected directory.
3. The frontend should support multiple database profiles, allowing users to switch between different repositories (e.g. local vs cloud) without losing data. Each profile would have its own settings and data.
4. Implement the pictures preview. The *preview.webp picture should be loaded in the db during the load to speed up display and scrolling. They are very small and could be shown in the left hand side models
5. There should be a "Picture Gallery" section in the model details panel that displays all the preview images for the versions of that model. Clicking on a preview image should open a larger view of the image, loaded directly from the local ZIP archive without needing to extract it to disk.
6. The tabs for the modelversions should show a green circle checkmark if the version is present in the local repository (i.e. if the corresponding ZIP file exists in the CivitAI repo path and if the corresponding safetensors file is also present, show a green circle checkmark. If the ZIP file is missing, show a red X. If the ZIP file is present but the safetensors file is missing, show a yellow warning triangle.
7. In the model link, the modelVersion is always 0 => only link to the model page, so the link ends with the model number.
8. The dropdown for the filters should have a "Clear" option to reset that filter. For example, in the "Model Type" filter, the dropdown would list all model types plus a "Clear" option at the top. Selecting "Clear" would deselect any selected model type and show all results.
9. The Pagination should have a 10 step forward/back. The number of hits per page should be configurable
10. the split divider between the results list and the details panel should be draggable to allow resizing. The divider position should be remembered per session.
11. The Tags Search is not working. Please implement it like it is said in SPEC_VIEWER.md
12. Show all Tags in the dataset detail view. Make portians expandable if we are lacking place. Make the TAB text for the models smaller.

	
	