## AI Prompt to create this Application. (Some tweeks done after the AI prompt I tried to update the AI prompt with those tweeks.)
 This is a WPF application, Implement MVVM model in C# .net 8.0.
Application name is "Sharepoint Video Downloader".
1.	create a label and textbox for download path at the top of the page. by default set it to "C:\Downloads", remember the last path and load it when application launch.
2.	create a button named "Get". when user press the get button build a string in following format. ffprobe -i  "{0}" -show_streams replacing {0} with text url value and truncate the value after format=dash& remove the & also.
3.	create a log text box add add the string in the log.
4.	run the string as shell command and take its output.
5.	the sample output is provided in file sample_output.txt in solution folder.
6.	from output take the values from [STREAM] and filter video only stream i.e having codec type video and display in a dropdown named ddl_video bind drop down value with index value and dropdown text with TAG:id value..
7.	from output take the values from [STREAM] and filter audio only stream i.e having codec type audio and display in a dropdown name ddl_audio bind drop down value with index value and dropdown text with TAG:id value.
8.	on dropdownlist selection or set the value of  a string named ffpmg_cmd by building following string "ffmpeg -i "{0}" -map 0:{1} -map 0:{2} -codec copy {3}.mp4" where {0} is the video url, {1} is dropdown ddl_video value, {2} is dropdown ddl_audio value, {3} is download path plus DateTime.Now.ToString("yyyyMMddHH:mm:ss") c# command.
9.	Create a textbox named txt_ffmpg and set the text property to ffmpg_cmd string value.
10.	Create a button Download video. by clicking it execute the external shell command in ffmpg_cmd string.
11.	On error displays the error in log textbox with Error: prefix. The log should be at the bottom of the page.
12. Add a clear log button at bottom of the page. Pressing this button clear the log and also clear the log textbox.
