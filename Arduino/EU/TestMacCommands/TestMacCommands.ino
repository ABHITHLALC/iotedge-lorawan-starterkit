
// This sample let you send test mac commands LinkCheck to your gateway

#include <LoRaWan.h>
//set to true to send confirmed data up messages
bool confirmed = false;
//application information, should be similar to what was provisiionned in the device twins
char * deviceId = "46AAC86800430028";
char * devAddr = "0228B1B1";
char * appSKey = "2B7E151628AED2A6ABF7158809CF4F3C";
char * nwkSKey = "3B7E151628AED2A6ABF7158809CF4F3C";


/*
  iot hub ABP tags for deviceid: 46AAC86800430028
    "desired": {
    "AppSKey": "2B7E151628AED2A6ABF7158809CF4F3C",
    "NwkSKey": "3B7E151628AED2A6ABF7158809CF4F3C",
    "DevAddr": "0228B1B1",
    "GatewayID" :"",
    "SensorDecoder" :"DecoderValueSensor"
    },
*/

//set initial datarate and physical information for the device
_data_rate_t dr = DR6;
_physical_type_t physicalType = EU868 ;

//internal variables
char data[10];
char buffer[256];
int lastCall = 0;


void setup(void)
{
  SerialUSB.begin(115200);
  while (!SerialUSB);
  lora.init();
  lora.setDeviceDefault();

  lora.setId(devAddr, deviceId, NULL);
  lora.setKey(nwkSKey, appSKey, NULL);
  lora.setDeciveMode(LWABP);
  lora.setDataRate(dr, physicalType);
  lora.setPort(0);
  lora.setChannel(0, 868.1);
  lora.setChannel(1, 868.3);
  lora.setChannel(2, 868.5);
  lora.setReceiceWindowFirst(0, 868.1);
  lora.setAdaptiveDataRate(false);
  lora.setDutyCycle(false);
  lora.setJoinDutyCycle(false);
  lora.setPower(1);
}

void loop(void)
{
  if ((millis() - lastCall) > 20000) {
    lastCall = millis();
    bool result = false;

    // Change to another cid to send another mac command
    unsigned char macCmd = 2;
    if (confirmed)
      result = lora.transferPacketWithConfirmed(&macCmd, 1, 10);
    else
      result = lora.transferPacket(&macCmd,1, 10);

    if (result)
    {
      short length;
      short rssi;
      SerialUSB.print("receiving ");
      memset(buffer, 0, sizeof(buffer));
      length = lora.receivePacket(buffer, sizeof(buffer), &rssi);

      if (length)
      {
        SerialUSB.print("Length is: ");
        SerialUSB.println(length);
        SerialUSB.print("RSSI is: ");
        SerialUSB.println(rssi);
        SerialUSB.print("Data is: ");
        for (unsigned char i = 0; i < length; i ++)
        {
          SerialUSB.print( char(buffer[i]));
        }
        SerialUSB.println();
      }
    }
  }
}
