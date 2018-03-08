#include "FastLED.h"


#if defined(FASTLED_VERSION) && (FASTLED_VERSION < 3001000)
#warning "Requires FastLED 3.1 or later; check github for latest code."
#endif

#define DATA_PIN    5
#define LED_TYPE    WS2811
#define COLOR_ORDER GRB
#define NUM_LEDS    300
#define BRIGHTNESS  88
#define ARRAY_SIZE(A) (sizeof(A) / sizeof((A)[0]))

String command = "";
uint8_t gHue = 0; // rotating "base color" 
byte ledRed = 255;
byte ledGreen = 0;
byte ledBlue = 0;
int TIMEOUT = 100;
CRGB leds[NUM_LEDS];

unsigned long lastTime;
unsigned long currentTime;

void setup() {
  delay(3000);

  Serial.begin(115200);

  FastLED.addLeds<LED_TYPE,DATA_PIN,COLOR_ORDER>(leds, NUM_LEDS).setCorrection(TypicalLEDStrip);

  FastLED.setBrightness(BRIGHTNESS);

  lastTime = millis();
}

void loop() 
{  
    currentTime = millis();

    if ((lastTime + TIMEOUT) > currentTime)
    {
      confetti();  
      FastLED.show();
      lastTime = currentTime;
    }
    
    // do some periodic updates
    //EVERY_N_MILLISECONDS( 200 ) { gHue++; }        
}

void serialEvent() 
{
    char c = Serial.read();

    if (c == '|')
    {
      parseCommand(command);
    }
    else
    {
      command += c;
    }
}

void parseCommand(String values)
{
  command = "";

  Serial.println("********");
  Serial.println("30 Values: " + values);
  
  String red = values.substring(0, 3);
  String grn = values.substring(3, 6);
  String blu = values.substring(6, 9);
  Serial.println("Red: " + red);
  Serial.println("Green: " + grn);
  Serial.println("Blue: " + blu); 
}

void confetti() 
{
  // random colored speckles that blink in and fade smoothly
  fadeToBlackBy( leds, NUM_LEDS, 10);
  int pos = random16(NUM_LEDS);
  leds[pos] += CHSV( gHue + random8(64), 200, 255);
  addGlitter(80);
}

void addGlitter( fract8 chanceOfGlitter) 
{
  if( random8() < chanceOfGlitter) {
    leds[ random16(NUM_LEDS) ] += CRGB::White;
  }
}