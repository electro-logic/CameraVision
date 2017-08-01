// Author: Leonardo Tazzini

// Camera Vision: FPGA video acquisition from D8M module
// See http://electro-logic.blogspot.it for articles about this project and updates

// NB: Some code in this project is taken or modified from Terasic D8M Example

#include <io.h>			// IORD, IOWR
#include <system.h>		// I2C_OPENCORES_MIPI_BASE, MIPI_CONTROLLER_0_BASE
#include <stdio.h>		// printf
#include <stdint.h>		// uintX_t
#include <unistd.h>		// usleep

#include "I2C_core.h"
#include "mipi_bridge_config.h"
#include "mipi_camera_config.h"
#include "stdio_ext.h"

#define I2C_SPEED		400000

// Camera Vision Commands

#define CMD_RD_REG 		0x01
#define CMD_WR_REG 		0x02
#define CMD_RESET_REG	0x04

#define CMD_WR_FOCUS	0x03
#define CMD_RD_FOCUS	0x07

#define CMD_CONFIG 		0x05
#define CMD_RD_IMG 		0x06

#define RESPONSE_OK	 	0xAA

// Setup MIPI Bridge and Camera
void config()
{
	//See OV8865 pag. 30/34 for power down sequence details
	// MIPI_PWDN_N = OV8865 PWDNB
	// MIPI_RESET_N = TC358748XBG RESX
	IOWR(MIPI_PWDN_N_BASE, 0x00, 0x00);
	IOWR(MIPI_RESET_N_BASE, 0x00, 0x00);
	usleep(2 * 1000);
	IOWR(MIPI_PWDN_N_BASE, 0x00, 0xFF);
	usleep(2 * 1000);
	IOWR(MIPI_RESET_N_BASE, 0x00, 0xFF);
	usleep(2000);

	oc_i2c_init_ex(I2C_OPENCORES_MIPI_BASE, ALT_CPU_FREQ, I2C_SPEED);
	mipi_bridge_init();
	usleep(500*1000);
	oc_i2c_uninit(I2C_OPENCORES_MIPI_BASE);
	oc_i2c_init_ex(I2C_OPENCORES_CAMERA_BASE, ALT_CPU_FREQ, I2C_SPEED);
	mipi_camera_init();
	usleep(1000);
	oc_i2c_uninit(I2C_OPENCORES_CAMERA_BASE);
}

void WaitFrame()
{
	// Wait until frame is captured
	uint32_t status = IORD(MIPI_CONTROLLER_0_BASE, 0x00);
	while((status  & 0x02)== 0x00)
	{
		usleep(1000 * 5);
		status = (IORD(MIPI_CONTROLLER_0_BASE, 0x00) & 0x02);
	}
}

int main()
{
	usleep(1000 * 10);

	config();

	// wait for one frame to adjust blc
	usleep(1000 * 500);

	IOWR(LED_BASE, 0x00, 0x01);
	uint16_t img_width = 3264;
	uint16_t img_height = 2448;

	uint16_t addr;
	uint8_t reg;

	oc_i2c_init_ex(I2C_OPENCORES_CAMERA_BASE, ALT_CPU_FREQ, I2C_SPEED);
	while(1){
		byte cmd = readByte();
		switch(cmd)
		{
			case CMD_RD_REG:
				addr = readUInt16();
				reg = mipi_camera_reg_read(addr);
				writeByte(reg);
				break;
			case CMD_WR_REG:
				addr = readUInt16();
				reg = readByte();
				mipi_camera_reg_write(addr, reg);
				break;
			case CMD_WR_FOCUS:
				addr = readUInt16();
				mipi_camera_reg_write_VCM149C(addr);
				writeByte(RESPONSE_OK);
				break;
			case CMD_RD_FOCUS:
				addr = mipi_camera_reg_read_VCM149C();
				writeBytes((byte*)&addr, 2);
				writeByte(RESPONSE_OK);
				break;
			case CMD_RESET_REG:
				mipi_camera_init();
				writeByte(RESPONSE_OK);
				break;
			case CMD_CONFIG:
				img_width= readUInt16();
				img_height= readUInt16();
				writeByte(RESPONSE_OK);
				break;
			case CMD_RD_IMG:
				IOWR(LED_BASE, 0x00, 0x00);

				IOWR(MIPI_CONTROLLER_0_BASE, 0x00, 0x00000001);	// Stop capture request
				WaitFrame();

				/*
				// Additional frame wait (wait 2 vsync, capture 3)
				IOWR(MIPI_CONTROLLER_0_BASE, 0x00, 0x00000000);	// Capture request again
				WaitFrame();
				IOWR(MIPI_CONTROLLER_0_BASE, 0x00, 0x00000000);	// Capture request again
				WaitFrame();
				*/

				// Read frame from memory and send to pc
				for(int rowIndex=0;rowIndex<img_height;rowIndex++)
				{
					// 16 bit per pixel
					// byte *bytes = (byte *)(SDRAM_BASE + (rowIndex * img_width * 2));
					// writeBytes(bytes, img_width * 2);
					byte *bytes = (byte *)(SDRAM_BASE + (rowIndex * img_width));
					writeBytes(bytes, img_width);
				}

				IOWR(MIPI_CONTROLLER_0_BASE, 0x00, 0x00000000);	// Capture request again
				IOWR(LED_BASE, 0x00, 0x01);
				writeByte(RESPONSE_OK);

				break;
		}
	};
	oc_i2c_uninit(I2C_OPENCORES_CAMERA_BASE);
	return 0;
}
