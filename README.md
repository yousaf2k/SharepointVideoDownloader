# Sharepoint Video Downloader
## Instructions:
1) Open video from SharePoint account or Teams OneDrive etc.,
2) In Chrome open Web inspector by pressing F12 then click Network tab then in filter enter videomanifest
3) In url list at bottom of filter check videomanifest and right click and copy this url
<img width="431" height="305" alt="image" src="https://github.com/user-attachments/assets/47440b23-04a9-46a8-91af-95ca56fb9a49" />
## Application Interface
<img width="1115" height="630" alt="image" src="https://github.com/user-attachments/assets/20b01046-c63b-4ec6-b389-44e69c492782" />
1) Set the Download Folder, Filename will be timestamp.mp4 i.e 20251108140201.mp4
2) Paste the videomanifest in Video URL.
3) Click the get button and wait for few seconds to get the video information.
4) Select the video and audio streams from drop downs.
5) Click the download video.
6) An external command will execute which downloads the video.
7) You can continue downloading more videos by pasting the video urls.
8) If you need to modify the ffmpg paramters just copy the command from text box and modify it and execute in new cmd window. You can modify the source code also as per your requirments.
