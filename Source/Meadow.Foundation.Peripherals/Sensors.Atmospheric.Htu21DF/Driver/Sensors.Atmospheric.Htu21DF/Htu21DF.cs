﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Meadow.Foundation.Sensors.Atmospheric
{
    public class Htu21d
    {
        #region Properties

        public int DEFAULT_SPEED => 400;

        #endregion Properties

        #region Member variables / fields

        private const byte TRIGGER_TEMP_MEASURE_NOHOLD = 0xF3;
        private const byte TRIGGER_HUMD_MEASURE_NOHOLD = 0xF5;
        private const byte TRIGGER_TEMP_MEASURE_HOLD = 0xE3;
        private const byte TRIGGER_HUMD_MEASURE_HOLD = 0xE5;

        private const byte WRITE_USER_REGISTER = 0xE6;
        private const byte READ_USER_REGISTER = 0xE7;
        private const byte SOFT_RESET = 0x0F;

        static II2cPeripheral htu21d;

        #endregion Member variables /fields


        #region Contstructors

        private Htu21d() { }
         //   htu21d = new I2CDevice(new I2CDevice.Configuration(HTDU21D_ADDRESS, DefaultClockRate));

        public Htu21d(II2cBus i2cBus, byte address = 0x40)
        {
            htu21d = new I2cPeripheral(i2cBus, address);
        }

        #endregion Constructors

        #region Methods

        public void Update()
        {
            ReadHumidity();
            ReadTemperature();
        }


        public async Task<float> ReadHumidity()
        {
            htu21d.WriteByte(TRIGGER_HUMD_MEASURE_NOHOLD);

            await Task.Delay(55);


            var humidityData = htu21d.ReadBytes(3);

            var msb = humidityData[0];
            var lsb = humidityData[1];
            var checksum = humidityData[2];

            uint humidity = ((uint)msb << 8) | (uint)lsb;

            humidity &= 0xFFFC; //Zero out the status bits but keep them in place

            //Given the raw humidity data, calculate the actual relative humidity
            float tempRH = humidity / (float)65536; //2^16 = 65536
            float realativeHumidity = (125 * tempRH) - 6; //From page 14

            return realativeHumidity;
        }


        public async Task<float> ReadTemperature()
        {
            htu21d.WriteByte(TRIGGER_TEMP_MEASURE_NOHOLD);

            await Task.Delay(55);

            var tempData = htu21d.ReadBytes(3);

            byte msb = tempData[0];
            byte lsb = tempData[1];
            byte checksum = tempData[2];

            uint temperature = ((uint)msb << 8) | (uint)lsb;

            temperature &= 0xFFFC; //Zero out the status bits but keep them in place

            //Given the raw temperature data, calculate the actual temperature
            var temp = temperature / (float)65536; //2^16 = 65536
            float realTemperature = (float)(-46.85 + (175.72 * temp)); //From page 14

            return realTemperature;
        }

        //Set sensor resolution
        /*******************************************************************************************/
        //Sets the sensor resolution to one of four levels
        //Page 12:
        // 0/0 = 12bit RH, 14bit Temp
        // 0/1 = 8bit RH, 12bit Temp
        // 1/0 = 10bit RH, 13bit Temp
        // 1/1 = 11bit RH, 11bit Temp
        //Power on default is 0/0
        void SetResolution(byte resolution)
        {
            byte userRegister = htu21d.ReadRegister(READ_USER_REGISTER); //Go get the current register state
                                                      //userRegister &= 0b01111110; //Turn off the resolution bits
                                                      //resolution &= 0b10000001; //Turn off all other bits but resolution bits
                                                      //userRegister |= resolution; //Mask in the requested resolution bits
            userRegister &= 0x73; //Turn off the resolution bits
            resolution &= 0x81; //Turn off all other bits but resolution bits
            userRegister |= resolution; //Mask in the requested resolution bits

            //Request a write to user register
            htu21d.WriteBytes(new byte[] { WRITE_USER_REGISTER }); //Write to the user register
            htu21d.WriteBytes(new byte[] { userRegister }); //Write the new resolution bits
        }

        #endregion Methods
    }
}