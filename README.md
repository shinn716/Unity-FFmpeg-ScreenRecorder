# Unity-FFmpeg-ScreenRecorder
Capture screen and audio (virtual audio and microphone)

<img src="https://github.com/shinn716/Unity-FFmpeg-ScreenRecorder/blob/main/img/drag02.gif" /></a>  
Right click on 'FFScreenRecorder' component, it will capture screen and mic both.
Export path is 'streamingassets/out'.

# List devices
'ffmpeg -list_devices true -f dshow -i dummy'

# Capture audio
'ffmpeg -f dshow -i audio="Device name" path-to-file\file-name.mp3' 
ex
'ffmpeg -f dshow -i audio="Microphone (Realtek(R) Audio)" d:\123.mp3'
'ffmpeg -f dshow -i audio="Microphone (Realtek(R) Audio)" -acodec libmp3lame "d:\out.mp3"

# Capture screen
ffmpeg -y -rtbufsize 100M -f gdigrab -t 00:00:30 -framerate 30 -probesize 10M -draw_mouse 1 -i desktop -c:v libx264 -r 30 -preset ultrafast -tune zerolatency -crf 25 -pix_fmt yuv420p d:/video_comapre2.mp4

# Capture screen and audio
ffmpeg -rtbufsize 1500M -f dshow -i audio="Microphone (Realtek(R) Audio)" -f -y -rtbufsize 100M -f gdigrab -t 00:00:30 -framerate 30 -probesize 10M -draw_mouse 1 -i desktop -c:v libx264 -r 30 -preset ultrafast -tune zerolatency -crf 25 -pix_fmt yuv420p "d:\ffmpeg_testing.mp4"

Hide mouse, Resolution 1920x1080
ffmpeg -rtbufsize 1500M -f dshow -i audio="Microphone (Realtek(R) Audio)" -f -y -rtbufsize 100M -f gdigrab -t 00:00:30 -video_size 1920x1080 -framerate 30 -probesize 10M -draw_mouse 0 -i desktop -c:v libx264 -r 30 -preset ultrafast -tune zerolatency -crf 25 -pix_fmt yuv420p "d:\ffmpeg_testing.mp4"