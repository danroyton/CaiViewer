1. Tags and Items per Page elements should be in the same row as the filters
2. The Items per Page element is not visible, the keys are overlapping
3. The 196 pixel high previw should be shown in double size and complete when hoovering over an entry. 
4. The preview picture should be 196 pixel high and 196*3 pixel wide, showing the 3 preview images for the model versions in a row. If there are less than 3 versions, the remaining space should be filled with a placeholder image.
5. The Image Gallery should be implemented as a horizontal scrollable list of full images. Clicking on a preview image should open a larger view of the image, loaded directly from the local ZIP archive without needing to extract it to disk. 
6. Files which are present in the local repository should be indicated with a green circle checkmark in the model version tab. The Markers ob the Tabs should show green checkmark if there is at least one file in the filesytem, and red x if no file of that version is present. if all files are present mark it with a green circlebox. expect the safetensors file or other files in the same directory as the zip file or in a configurable path which will then be scanned recursively.
7. In the settingspage, add a Button "create database" in order to create a cai.db File in a folder to be chosen from a folder select box. 
8. Make sure as soon as the first DB ist there, that the db chooser is showing that DB entry with full path instead of "default". Make sure the field is longer. Do a auto reload if anither DB is chosen.
9. Put the discription to the end of the details page and make the complete Page scrollable if it is too long.
10. Put the created by and the creator behind the model name in the details page. Put the three Buttons for the model links (CivitAI, HuggingFace, CreaTIvity) in the same line as the model name and creator. Make the buttons smaller (size like the type) and show them on the right hand side.
11. Make the creator also searchable
12. Show the base model and base model type before the tags list.
