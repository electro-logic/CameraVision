Camera Vision

Low-level FPGA / D8M / OV8865 interfacing with PC throught USB
Read http://electro-logic.blogspot.it for more documentation

![alt text](https://raw.githubusercontent.com/electro-logic/CameraVision/master/docs/de0-nano_d8m.jpg)

![alt text](https://raw.githubusercontent.com/electro-logic/CameraVision/master/docs/camera_vision_gui.png)

Requirements:

- Visual Studio 2017 (.NET 4.5)
- Altera Quartus 17

Supported Hardware:

- DE0-Nano
- D8M

Notes:

Power consumption of DE0-Nano with D8M connected throught USB: 0.35A

Quick start:

1) Connect D8M into GPIO0 of DE0-Nano like shown into images into \doc folder 
2) Connect DE0-Nano to PC with USB cable bundled
3) Load with Quartus Programmer \eda\de0-nano\output_files\DE0_NANO_D8M.sof into DE0-Nano
4) Wait that LED0 turn on and launch \sw\CameraVision\CameraVision\bin\Release\CameraVision.exe
5) Press Update into Image panel to take a new image

F.A.Q.

Q) When I launch CameraVision.exe image is corrupted.
A) Try to press KEY0 on DE0-NANO to reset the system and try again to launch the software