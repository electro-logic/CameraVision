// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraVision
{
    public class MainWindowVM : ObjectModel
    {
        private OV8865 _sensor;
        private WriteableBitmap _image;
        DemosaicingAlgorithms _currentDemosaicingAlgorithm;
        VideoSetting _currentVideoSetting;
        double[] _ccm;
        double _ccmScale;
        bool _isColorCorrectionEnabled;
        PointCollection _histogramPoints;

        public ObservableCollection<Register> Registers { get; set; }
        public ICommand DownloadImageCommand { get; set; }
        public ICommand SaveImageCommand { get; set; }
        public ICommand ReadRegistersCommand { get; set; }
        public ICommand LoadRegistersCommand { get; set; }
        public ICommand SaveRegistersCommand { get; set; }
        public ICommand FocusBracketingCommand { get; set; }
        public ICommand ExposureBracketingCommand { get; set; }

        public List<VideoSetting> VideoSettings { get; set; }
        public DemosaicingAlgorithms[] DemosaicingAlgorithms
        {
            get
            {
                return (DemosaicingAlgorithms[])Enum.GetValues(typeof(CameraVision.DemosaicingAlgorithms));
            }
        }

        public MainWindowVM()
        {
            try
            {
                _sensor = new OV8865();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\nFPGA with D8M and right bitstream is connected to PC?");
                Environment.Exit(-1);
            }

            VideoSettings = new List<VideoSetting>()
            {
                new VideoSetting(){ Description="3264x2448 8MP", Registers=new List<Register>(){
                    new Register(0x3808,0x0C),
                    new Register(0x3809,0xC0),
                    new Register(0x380A,0x09),
                    new Register(0x380B,0x90),
                    new Register(0x3821,0x00),
                } },
                new VideoSetting(){ Description="1632x1224 2MP (2x2 binning)", Registers=new List<Register>(){
                    new Register(0x3808,0x06),
                    new Register(0x3809,0x60),
                    new Register(0x380A,0x04),
                    new Register(0x380B,0xC8),
                    new Register(0x3814,0x03),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x03),
                    new Register(0x382B,0x01),
                    new Register(0x3821,0x70),
                    new Register(0x3830,0x04),
                    new Register(0x3836,0x01),
                } },
                new VideoSetting(){ Description="1920x1080 2MP FHD", Registers=new List<Register>(){
                    new Register(0x3808,0x07),
                    new Register(0x3809,0x80),
                    new Register(0x380A,0x04),
                    new Register(0x380B,0x38),
                } },
                new VideoSetting(){ Description="1366x768 1MP HD", Registers=new List<Register>(){
                    new Register(0x3808,0x05),
                    new Register(0x3809,0x56),
                    new Register(0x380A,0x03),
                    new Register(0x380B,0x00),
                } },
                new VideoSetting(){ Description="1024x768 0.7MP XGA", Registers=new List<Register>(){
                    new Register(0x3808,0x04),
                    new Register(0x3809,0x00),
                    new Register(0x380A,0x03),
                    new Register(0x380B,0x00),
                } },
                new VideoSetting(){ Description="816x612 0.5MP (4x4 skipping)", Registers=new List<Register>(){
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x30),
                    new Register(0x380A,0x02),
                    new Register(0x380B,0x64),
                    new Register(0x3814,0x07),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x07),
                    new Register(0x382B,0x01),
                    new Register(0x3830,0x08),
                    new Register(0x3836,0x02),
                } },
                new VideoSetting(){ Description="800x600 SVGA", Registers=new List<Register>(){
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x20),
                    new Register(0x380A,0x02),
                    new Register(0x380B,0x58),
                } },
                new VideoSetting(){ Description="800x480", Registers=new List<Register>(){
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x20),
                    new Register(0x380A,0x01),
                    new Register(0x380B,0xE0),
                } },
                new VideoSetting(){ Description="640x480 0.3MP VGA (4x4 skipping)", Registers=new List<Register>(){
                    new Register(0x3808,0x02),
                    new Register(0x3809,0x80),
                    new Register(0x380A,0x01),
                    new Register(0x380B,0xE0),
                    new Register(0x3814,0x07),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x07),
                    new Register(0x382B,0x01),
                    new Register(0x3830,0x08),
                    new Register(0x3836,0x02),
                } },
                new VideoSetting(){ Description="640x480 0.3MP VGA", Registers=new List<Register>(){
                    new Register(0x3808,0x02),
                    new Register(0x3809,0x80),
                    new Register(0x380A,0x01),
                    new Register(0x380B,0xE0),
                } },
                new VideoSetting(){ Description="320x240 CGA", Registers=new List<Register>(){
                    new Register(0x3808,0x01),
                    new Register(0x3809,0x40),
                    new Register(0x380A,0x00),
                    new Register(0x380B,0xF0),
                } }
            };

            CurrentDemosaicingAlgorithm = CameraVision.DemosaicingAlgorithms.SIMPLE_INTERPOLATION;
            CurrentVideoSetting = VideoSettings[5];

            Ccm = new double[9];

            // Identity
            Ccm[0] = 1.0;
            Ccm[4] = 1.0;
            Ccm[8] = 1.0;


            Ccm[0] = 1.5344;
            Ccm[1] = 0.241;
            Ccm[2] = -0.2618;
            Ccm[3] = -0.2334;
            Ccm[4] = 1.3766;
            Ccm[5] = -0.4828;
            Ccm[6] = -0.0335;
            Ccm[7] = -0.4097;
            Ccm[8] = 2.1413;

            CcmScale = 1.0;

            DownloadImageCommand = new DelegateCommand(DownloadImage);
            SaveImageCommand = new DelegateCommand(SaveImage);
            ReadRegistersCommand = new DelegateCommand(ReadRegisters);
            LoadRegistersCommand = new DelegateCommand(LoadRegisters);
            FocusBracketingCommand = new DelegateCommand(FocusBracketing);
            //SaveRegistersCommand = new DelegateCommand(SaveRegisters);
            ExposureBracketingCommand = new DelegateCommand(ExposureBracketing);
            HistogramPoints = new PointCollection();

            Registers = new ObservableCollection<Register>();
        }

        private async void ExposureBracketing()
        {
            _sensor.WriteReg(0x3500, 0);
            _sensor.WriteReg(0x3501, 0);
            _sensor.WriteReg(0x3502, 0);
            for (byte i = 0; i < 255; i++)
            {
                _sensor.WriteReg(0x3502, (byte)(i));
                await Task.Delay(500);
                DownloadImage();
                Image.Save(i + ".png");
            }
        }

        private void FocusBracketing()
        {
            Focus = 1000;
            while (Focus > 500)
            {
                DownloadImage();
                Image.Save(Focus + ".png");
                Focus = Focus - 10;
            }
        }

        public void LoadRegisters()
        {
            var dlg = new OpenFileDialog();
            dlg.FileName = "ov8865";
            dlg.DefaultExt = ".ovregs";
            dlg.Filter = "OV8865 registers (.ovregs)|*.ovregs";
            var res = dlg.ShowDialog();
            if (res == true)
            {
                string filename = dlg.FileName;
                string[] lines = File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    var tokens = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    UInt16 addr = UInt16.Parse(tokens[0]);
                    byte val = byte.Parse(tokens[1]);
                    _sensor.WriteReg(addr, val);
                }
            }
        }

        private void AddRegister(UInt16 addr, string desc = "")
        {
            var reg = new Register(addr, _sensor.ReadReg(addr).ToString("X"), desc);
            reg.PropertyChanged += Reg_PropertyChanged;
            Registers.Add(reg);
        }

        private void Reg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var reg = sender as Register;
            _sensor.WriteReg(reg.Address, Convert.ToByte(reg.Value, 16));
        }

        public void ReadRegisters()
        {
            Registers.Clear();

            //AddRegister(0x0100, "OV8865_SC_CTRL0100");
            //AddRegister(0x0103, "OV8865_SC_CTRL0103");

            // PLL
            //AddRegister(0x0300, "PLL_CTRL_0");
            //AddRegister(0x0301, "PLL_CTRL_1");
            //AddRegister(0x0302, "PLL_CTRL_2");
            //AddRegister(0x0303, "PLL_CTRL_3");
            //AddRegister(0x0304, "PLL_CTRL_4");
            //AddRegister(0x0305, "PLL_CTRL_5");
            //AddRegister(0x0306, "PLL_CTRL_6");
            //AddRegister(0x0307, "PLL_CTRL_7");
            //AddRegister(0x0308, "PLL_CTRL_8");
            //AddRegister(0x0309, "PLL_CTRL_9");
            //AddRegister(0x030A, "PLL_CTRL_A");
            //AddRegister(0x030B, "PLL_CTRL_B");
            //AddRegister(0x030C, "PLL_CTRL_C");
            //AddRegister(0x030D, "PLL_CTRL_D");
            //AddRegister(0x030E, "PLL_CTRL_E");
            //AddRegister(0x030F, "PLL_CTRL_F");
            //AddRegister(0x0310, "PLL_CTRL_10");
            //AddRegister(0x0311, "PLL_CTRL_11");
            //AddRegister(0x0312, "PLL_CTRL_12");
            //AddRegister(0x031B, "PLL_CTRL_1B");
            //AddRegister(0x031C, "PLL_CTRL_1C");
            //AddRegister(0x031E, "PLL_CTRL_1E");
            //AddRegister(0x3106, "SCLK_DIV, SCLK_PRE_DIV");
            //AddRegister(0x3007, "R ISPOUT BITSEL");
            //AddRegister(0x3020, "PCLK_DIV");
            //AddRegister(0x3032, "MUX PLL_SYS_CLK");
            //AddRegister(0x3033, "MUX DAC_SYS_CLK");

            // Image Windowing Control
            AddRegister(0x3808, "H_OUTPUT_SIZE 11:8");
            AddRegister(0x3809, "H_OUTPUT_SIZE 7:0");
            AddRegister(0x380A, "V_OUTPUT_SIZE 11:8");
            AddRegister(0x380B, "V_OUTPUT_SIZE 7:0");

            AddRegister(0x380C, "TIMING_HTS 15:8");
            AddRegister(0x380D, "TIMING_HTS 7:0");
            AddRegister(0x380E, "TIMING_VTS 15:8");
            AddRegister(0x380F, "TIMING_VTS 7:0");

            AddRegister(0x3820, "TIMING_FORMAT1");

            // Binning
            AddRegister(0x3821, "TIMING_FORMAT2");
            AddRegister(0x3814, "X_ODD_INC");
            AddRegister(0x3815, "X_EVEN_INC");
            AddRegister(0x382A, "Y_ODD_INC");
            AddRegister(0x382B, "Y_EVEN_INC");

            // DSP top
            AddRegister(0x5000, "DSP CTRL00");
            AddRegister(0x5001, "DSP CTRL01");  // BLC function enable
            AddRegister(0x5002, "DSP CTRL02");  // Variopixel function enable
            AddRegister(0x5003, "DSP CTRL03");
            AddRegister(0x5004, "DSP CTRL04");
            AddRegister(0x5005, "DSP CTRL05");
            AddRegister(0x501F, "DSP CTRL1F");
            AddRegister(0x5025, "DSP CTRL25");
            AddRegister(0x5041, "DSP CTRL41");
            AddRegister(0x5043, "DSP CTRL43");

            // Pre DSP
            AddRegister(0x5E00, "PRE CTRL00");
            AddRegister(0x5E01, "PRE CTRL01");

            // Defective Pixel Cancellation (DPC)
            AddRegister(0x5000, "ISP CTRL00");

            // Window Cut (WINC)
            //AddRegister(0x5A00, "WINC CTRL00");
            //AddRegister(0x5A01, "WINC CTRL01");
            //AddRegister(0x5A02, "WINC CTRL02");
            //AddRegister(0x5A03, "WINC CTRL03");
            //AddRegister(0x5A04, "WINC CTRL04");
            //AddRegister(0x5A05, "WINC CTRL05");
            //AddRegister(0x5A06, "WINC CTRL06");
            //AddRegister(0x5A07, "WINC CTRL07");
            //AddRegister(0x5A08, "WINC CTRL08");

            // Manual White Balance (MWB)
            AddRegister(0x5018, "ISP CTRL18");
            AddRegister(0x5019, "ISP CTRL19");
            AddRegister(0x501A, "ISP CTRL1A");
            AddRegister(0x501B, "ISP CTRL1B");
            AddRegister(0x501C, "ISP CTRL1C");
            AddRegister(0x501D, "ISP CTRL1D");
            AddRegister(0x501E, "ISP CTRL1E");

            // Manual Exposure Compensation (MEC) / Manual Gain Compensation (MGC)
            AddRegister(0x3500, "AEC EXPO 19:16");
            AddRegister(0x3501, "AEC EXPO 15:8");
            AddRegister(0x3502, "AEC EXPO 7:0");
            AddRegister(0x3503, "AEC MANUAL");
            //AddRegister(0x3505, "GCVT OPTION");
            //AddRegister(0x3507, "AEC GAIN SHIFT");
            AddRegister(0x3508, "AEC GAIN 12:8");
            AddRegister(0x3509, "AEC GAIN 7:0");
            AddRegister(0x350A, "AEC DIGIGAIN 13:6");
            AddRegister(0x350B, "AEC DIGIGAIN 5:0");

            // System Control
            //AddRegister(0x300A, "CHIP ID 23:16");
            //AddRegister(0x300B, "CHIP ID 15:8");
            //AddRegister(0x300C, "CHIP ID 7:0");

            // Timing Control Registers
            //AddRegister(0x3822, "REG22");
            //AddRegister(0x382C, "BLC COL ST L");
            //AddRegister(0x382D, "BLC COL END L");
            //AddRegister(0x382E, "BLC COL ST R");
            //AddRegister(0x382F, "BLC COL END R");
            //AddRegister(0x3830, "BLC NUM OPTION");
            //AddRegister(0x3831, "BLC NUM MAN");
            //AddRegister(0x3836, "ZLINE NUM OPTION");

            // BLC Control
            //AddRegister(0x4000, "BLC CTRL00");
            AddRegister(0x4004, "BLC CTRL04 target 15:8");
            AddRegister(0x4005, "BLC CTRL05 target 7:0");

            AddRegister(0x4300, "CLIP MAX HI");
            AddRegister(0x4301, "CLIP MIN HI");
            //AddRegister(0x4302, "CLIP LO");
            //AddRegister(0x4303, "FORMAT CTRL 3");


            // OTP
            // AddRegister(0x3D81, "OTP_REG85");

            // Update GUI elements
            OnPropertyChanged("FPS");
            OnPropertyChanged("ExposureMs"); OnPropertyChanged("Exposure");
            OnPropertyChanged("AnalogGain"); OnPropertyChanged("ISO");
            OnPropertyChanged("IsWhiteBalanceEnabled");
            OnPropertyChanged("MWBGainRed"); OnPropertyChanged("MWBGainGreen"); OnPropertyChanged("MWBGainBlue");
            
        }

        public double Exposure
        {
            get
            {
                return (double)(_sensor.GetExposure());
            }
            set
            {
                _sensor.SetExposure((UInt16)(value));
                UInt16 vts = _sensor.GetVTS();  // dummy lines
                UInt16 hts = _sensor.GetHTS();  // extra lines

                // Max exposure = dummy lines - 4
                UInt16 maxExposure = (UInt16)(vts);

                // Exposure = Shutter + extra lines 
                UInt16 exposure = (UInt16)(value + hts);
                if (maxExposure < exposure)
                {
                    _sensor.SetVTS((UInt16)(exposure + 4));
                }

                OnPropertyChanged("Exposure");
                OnPropertyChanged("ExposureMs");
            }
        }

        public double Focus
        {
            get { return (double)(_sensor.GetFocus()); }
            set { _sensor.SetFocus((UInt16)value); OnPropertyChanged("Focus"); }
        }

        public double AnalogGain
        {
            get { return (double)(_sensor.GetAnalogGain() + 1); }
            set { _sensor.SetAnalogGain((byte)(value - 1)); OnPropertyChanged("AnalogGain"); OnPropertyChanged("ISO"); }
        }

        public double ISO
        {
            get { return AnalogGain * 100; }
        }

        public double MWBGainRed
        {
            get { return (double)(_sensor.GetMWBGainRed() / 1024); }
            set { _sensor.SetMWBGainRed((UInt16)(value * 1024)); OnPropertyChanged("MWBGainRed"); }
        }

        public double MWBGainGreen
        {
            get { return (double)(_sensor.GetMWBGainGreen() / 1024); }
            set { _sensor.SetMWBGainGreen((UInt16)(value * 1024)); OnPropertyChanged("MWBGainGreen"); }
        }

        public double MWBGainBlue
        {
            get { return (double)(_sensor.GetMWBGainBlue() / 1024); }
            set { _sensor.SetMWBGainBlue((UInt16)(value * 1024)); OnPropertyChanged("MWBGainBlue"); }
        }

        public WriteableBitmap Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        public VideoSetting CurrentVideoSetting
        {
            get
            {
                return _currentVideoSetting;
            }

            set
            {
                _currentVideoSetting = value;
                OnPropertyChanged("CurrentVideoSetting");

                //_sensor.WriteReg(0x0100, 0x00); // Software Standby                

                // No skipping or binning                

                _sensor.WriteReg(0x3814, 0x01);
                Thread.Sleep(200);
                _sensor.WriteReg(0x3815, 0x01);
                Thread.Sleep(200);
                _sensor.WriteReg(0x382A, 0x01);
                Thread.Sleep(200);
                _sensor.WriteReg(0x382B, 0x01);
                Thread.Sleep(200);

                _sensor.WriteReg(0x3821, 0x00); // 48
                _sensor.WriteReg(0x3830, 0x08);
                _sensor.WriteReg(0x3836, 0x02);

                //_sensor.WriteReg(0x0100, 0x01); // Streaming

                foreach (var reg in _currentVideoSetting.Registers)
                {
                    _sensor.WriteReg(reg.Address, byte.Parse(reg.Value, System.Globalization.NumberStyles.HexNumber));
                    if ((reg.Address == 0x3814) | (reg.Address == 0x3815) | (reg.Address == 0x382A) | (reg.Address == 0x382B))
                    {
                        Thread.Sleep(200);
                    }
                }

                int ImageWidthH = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x3808).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageWidthL = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x3809).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageWidth = (ImageWidthH << 8) | ImageWidthL;
                int ImageHeightH = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x380A).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageHeightL = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x380B).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageHeight = (ImageHeightH << 8) | ImageHeightL;
                _sensor.Config((ushort)ImageWidth, (ushort)ImageHeight);

                Image = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, PixelFormats.Bgr32, BitmapPalettes.WebPalette);
            }
        }

        public DemosaicingAlgorithms CurrentDemosaicingAlgorithm
        {
            get
            {
                return _currentDemosaicingAlgorithm;
            }
            set
            {
                _currentDemosaicingAlgorithm = value;
                OnPropertyChanged("CurrentDemosaicingAlgorithm");
            }
        }

        public double[] Ccm
        {
            get
            {
                return _ccm;
            }

            set
            {
                _ccm = value;
                OnPropertyChanged("Ccm");
            }
        }

        public bool IsColorCorrectionEnabled
        {
            get
            {
                return _isColorCorrectionEnabled;
            }

            set
            {
                _isColorCorrectionEnabled = value;
                OnPropertyChanged("IsColorCorrectionEnabled");
            }
        }

        public bool IsWhiteBalanceEnabled
        {
            get
            {
                return _sensor.GetWhiteBalanceEnable();
            }
            set
            {
                _sensor.SetWhiteBalanceEnable(value);
                OnPropertyChanged("IsWhiteBalanceEnabled");
            }
        }

        public double ExposureMs
        {
            get
            {
                UInt16 hts = _sensor.GetHTS();  // extra lines
                return Math.Round((1.0 / 125000.0) * (double)(hts) * (Exposure),2);
            }
        }

        public double FPS
        {
            get
            {
                UInt16 hts = _sensor.GetHTS();  // extra lines
                UInt16 vts = _sensor.GetVTS();  // dummy lines
                // System Clock (SCLK) of sensor is set at 166.66 MHz
                double fps = 166666666.0 / (hts * vts);
                return Math.Round(fps, 2);
            }
        }

        public PointCollection HistogramPoints
        {
            get
            {
                return _histogramPoints;
            }

            set
            {
                _histogramPoints = value;
                OnPropertyChanged("HistogramPoints");
            }
        }

        public double CcmScale
        {
            get
            {
                return _ccmScale;
            }

            set
            {
                _ccmScale = value;
                OnPropertyChanged("CcmScale");
            }
        }

        private static double Saturate(double val, double min, double max)
        {
            if (val > max)
                return max;
            if (val < min)
                return min;
            return val;
        }

        public void DownloadImage()
        {
            int imageWidth = (int)Image.Width;
            int imageHeight = (int)Image.Height;
            byte[] rawPixels = _sensor.GetImage(imageWidth, imageHeight);

            // Create Histogram
            int[] histogramValues = new int[256];

            for (int pixelIndex = 0; pixelIndex < imageWidth * imageHeight; pixelIndex++)
            {
                histogramValues[rawPixels[pixelIndex]]++;
            }
            HistogramPoints.Clear();
            double max = histogramValues.Max();
            HistogramPoints.Add(new Point(0, 0));
            for (int i = 0; i < histogramValues.Length; i++)
            {
                double x = i;
                double y = 100.0 * histogramValues[i] / max;
                HistogramPoints.Add(new Point(x, y));
            }
            double endX = (histogramValues.Length - 1);
            HistogramPoints.Add(new Point(endX, 0));

            HistogramPoints = new PointCollection(HistogramPoints);
            OnPropertyChanged("HistogramPoints");

            int[] colorPixels = null;
            switch (CurrentDemosaicingAlgorithm)
            {
                case CameraVision.DemosaicingAlgorithms.SIMPLE_INTERPOLATION:
                    colorPixels = BayerAlgoritms.Demosaic(rawPixels, (int)Image.Width, (int)Image.Height);
                    break;
                case CameraVision.DemosaicingAlgorithms.COLOR_RAW:
                    colorPixels = BayerAlgoritms.ColorRaw(rawPixels, (int)Image.Width, (int)Image.Height);
                    break;
                case CameraVision.DemosaicingAlgorithms.GRAY_RAW:
                    colorPixels = BayerAlgoritms.GrayRaw(rawPixels, (int)Image.Width, (int)Image.Height);
                    break;
                default:
                    throw new Exception("Demosaicing algorithm not implemented");
            }

            if (IsColorCorrectionEnabled)
            {
                for (int y = 0; y < Image.Height; y++)
                {
                    for (int x = 0; x < Image.Width; x++)
                    {
                        int pixelIndex = y * (int)Image.Width + x;

                        byte[] colorBytes = BitConverter.GetBytes(colorPixels[pixelIndex]); // BGR32

                        byte r = colorBytes[2];
                        byte g = colorBytes[1];
                        byte b = colorBytes[0];

                        // Don't correct completely black pixels
                        if ((r == 0) && (g == 0) && (b == 0))
                            continue;

                        // Don't correct completely saturated pixels
                        if ((r == 255) && (g == 255) && (b == 255))
                            continue;

                        var c = System.Drawing.Color.FromArgb(r, g, b);
                        if (c.GetBrightness() > 0.75)
                        {
                            colorPixels[pixelIndex] = BitConverter.ToInt32(new byte[] { 255, 255, 255, 255 }, 0);
                            continue;
                        }

                        byte Cr = (byte)Saturate(Ccm[0] * CcmScale * r + Ccm[1] * CcmScale * g + Ccm[2] * CcmScale * b, 0, 255);
                        byte Cg = (byte)Saturate(Ccm[3] * CcmScale * r + Ccm[4] * CcmScale * g + Ccm[5] * CcmScale * b, 0, 255);
                        byte Cb = (byte)Saturate(Ccm[6] * CcmScale * r + Ccm[7] * CcmScale * g + Ccm[8] * CcmScale * b, 0, 255);

                        colorPixels[pixelIndex] = BitConverter.ToInt32(new byte[] { Cb, Cg, Cr, 255 }, 0);
                    }
                }
            }

            Image.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), colorPixels, imageWidth * PixelFormats.Bgr32.BitsPerPixel / 8, 0);
        }

        public void SaveImage()
        {
            var dlg = new SaveFileDialog();
            dlg.FileName = "camera_" + DateTime.Now.ToString("hhmmss") + ".png";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG image (.png)|*.png";
            var res = dlg.ShowDialog();
            if (res == true)
            {
                string filename = dlg.FileName;
                Image.Save(filename);
            }
        }
    }
}
