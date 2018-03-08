#include "FastLED.h"

#if defined(FASTLED_VERSION) && (FASTLED_VERSION < 3001000)
#warning "Requires FastLED 3.1 or later; check github for latest code."
#endif

#define DATA_PIN    5
#define LED_TYPE    WS2811
#define COLOR_ORDER GRB
#define NUM_LEDS    300
#define ARRAY_SIZE(A) (sizeof(A) / sizeof((A)[0]))

String serialData = "";
byte ledRed = 255;
byte ledGreen = 0;
byte ledBlue = 0;
byte ledBrightness = 88;
int TIMEOUT = 100;
uint8_t gHue = 0;

CRGB leds[NUM_LEDS];

unsigned long lastTime;
unsigned long currentTime;

void setup() {
  delay(3000);

  Serial.begin(115200);

  FastLED.addLeds<LED_TYPE,DATA_PIN,COLOR_ORDER>(leds, NUM_LEDS).setCorrection(TypicalLEDStrip);

  FastLED.setBrightness(ledBrightness);

  lastTime = millis();
}

void loop() 
{  
    currentTime = millis();

   if ((currentTime - lastTime) > TIMEOUT)
    {
      confetti();  
      FastLED.show();
      lastTime = currentTime;
    }      
}

void serialEvent() 
{
    char c = Serial.read();

    if (c == '|')
    {
      parseCommand(serialData);
    }
    else
    {
      serialData += c;
    }
}

void parseCommand(String values)
{
    serialData = "";

    String sValue;
    byte position = 0;
    char sz[] = "012345678901234567890";
    char buf[sizeof(sz)];
    values.toCharArray(buf, sizeof(buf));
    char *p = buf;
    char *str;
    
    while ((str = strtok_r(p, ";", &p)) != NULL) //delimiter is semicolon
    {
      sValue = str;
      
      switch (position)
      {
        case 0: //Brightness
          ledBrightness = sValue.toInt();
          break;
          
        case 1:
          ledRed = sValue.toInt();
          break;

        case 2:
          ledGreen = sValue.toInt();
          break;        

        case 3:
          ledBlue = sValue.toInt();
          break;            
      }
      position++;
    }

    //Set values
    FastLED.setBrightness(ledBrightness);

    for( int i = 0; i < NUM_LEDS; i++) { 
      leds[i] = CRGB(ledRed, ledGreen, ledBlue);
    }

    FastLED.show();
}

void confetti() 
{
  fadeToBlackBy( leds, NUM_LEDS, 10);
  int pos = random16(NUM_LEDS);
  leds[pos] = CRGB(ledRed, ledGreen, ledBlue);
  addGlitter(80);
}

void addGlitter( fract8 chanceOfGlitter) 
{
  if( random8() < chanceOfGlitter) {
    leds[ random16(NUM_LEDS) ] += CRGB::White;
  }
}