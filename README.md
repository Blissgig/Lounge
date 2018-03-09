# Lounge
A multi-monitor, multi-media, multi-sensory application.

This is not your standard media player.  This is more of an art project/multi-media experience.

I have a multi-monitor setup at home and created a hacked together piece of software to play media while I code, play games or just in the background.  This is a replacement for that software, but much more.  It connects to addressable LEDs to affect them based on situations within the application.

March 9, 2018: Happy to report second alpha version ready for testing.

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
* Videos are played without sound, this is to avoid playback conflicts with the audio/music.

Keyboard Shortcuts:
* A = Select All files
* B = Back one folder level
* H = Home folders
* P = Play
* Escape = close application
* Volume Mute = Mute / Unmute volume

------------------------------------------------
External Assets
* Icons: https://www.iconfinder.com/iconsets/snipicons
* Application icon converted from PNG to ICO via: http://icoconvert.com/
* Code Libraries:
 - Bass.Net:  https://www.nuget.org/packages/Bass.Net/  (Used for audio visualization)
* Hardware: 
 - LEDs: https://www.amazon.com/gp/product/B01CDTECSG/ref=oh_aui_detailpage_o00_s00?ie=UTF8&psc=1
 - Arduino Uno: https://www.amazon.com/gp/product/B008GRTSV6/ref=oh_aui_detailpage_o00_s00?ie=UTF8&psc=1
 - Power source for Arduino: https://www.amazon.com/gp/product/B06Y1LF8T5/ref=oh_aui_detailpage_o00_s00?ie=UTF8&psc=1   
 - Power source for LEDs: https://www.amazon.com/gp/product/B01D8FLWGE/ref=oh_aui_detailpage_o04_s00?ie=UTF8&psc=1
Note: It is VERY important that when working with an external power source like the one I linked that you be VERY careful and have knowledge of electricity.  Beware, electricity can kill.   This power was needed as I am using two sets of the LEDs listed.  I was able to just use the power supply for the Arduino when testing with just one LED strip.

------------------------------------------------
Technical Support:
* If the Arduino is not powered up BEFORE starting Lounge, then Lounge will not be able to access the LEDs
