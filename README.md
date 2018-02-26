# Lounge
A multi-monitor, multi-media, multi-sensory application

I have a multi-monitor setup at home and created a hacked together piece of software to play media while I code, play games or just in the background.  This is a replacement for that software, but much more.  It will connect to addressable LEDs to affect them based on situations within the application.


------------------------------------------------
Specifications:
* Allow media playback on multiple monitors, with option for primary monitor.  Not displaying on the primary monitor allows the user to continue playing games, surfing the web, etc.
* Each Window can have multiple visual media displayed.  The media displayed is selected via the Lounge Engine (LoungeEngine.cs) at random for display for a random period of time.
* Random time range settings available.
* Color range settings available.
* Users can add music/audio for playback.
* Multiple audio visualizations, multiple select available.
* LEDs, if using Arduino/LEDs, will change color to match current application/visualization color.
* Optional LED patterns
* Videos are played without sound, this is to avoid playback conflicts with the audio/music.  This is not a standard media player.


------------------------------------------------
External Assets
* Icons
 - App:    https://www.iconfinder.com/icons/173014/fire_icon#size=512 converted to ico via: http://icoconvert.com/
 - Audio:  https://www.iconfinder.com/icons/173060/music_icon#size=256
 - Photo:  https://www.iconfinder.com/icons/173070/picture_icon#size=256
 - Video:  https://www.iconfinder.com/icons/173123/video_icon#size=256
 - Folder: https://www.iconfinder.com/icons/173016/close_folder_icon#size=256
 
* Code Libraries
 - Bass.Net:  https://www.nuget.org/packages/Bass.Net/  (Used for audio visualization)
 
* Hardware 
 - LEDs: https://www.amazon.com/gp/product/B01CDTECSG/ref=oh_aui_detailpage_o00_s00?ie=UTF8&psc=1
 - Arduino Uno: https://www.amazon.com/gp/product/B008GRTSV6/ref=oh_aui_detailpage_o00_s00?ie=UTF8&psc=1
 - Power source for Arduino, also important for the LEDs: https://www.amazon.com/gp/product/B06Y1LF8T5/ref=oh_aui_detailpage_o00_s00?ie=UTF8&psc=1
