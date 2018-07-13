using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Accord.Audio;
using Accord.DirectSound;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;

namespace WebcamCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isRecording = false;
        VideoCaptureDevice videoSource;
        AudioCaptureDevice AudioSource;
        ConnectorHub.ConnectorHub myConnector;
        System.DateTime sartRecordingTime;
        System.DateTime lastRecordingVideoTime;
        System.DateTime lastRecordingSoundTime;
        VideoFileWriter vf;

        VideoFileWriter writer;
        string filename;
        int i;

        public MainWindow()
        {
            InitializeComponent();
            i = 0;
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();

            vf = new VideoFileWriter();

            try
            {
                AudioSource = new AudioCaptureDevice();
                AudioDeviceInfo info = null;
                var adc = new AudioDeviceCollection(AudioDeviceCategory.Capture);
                foreach (var ad in adc)
                {
                    string desc = ad.Description;
                    if (desc.IndexOf("Audio") > -1)
                    {
                        info = ad;
                    }
                }
                if (info == null)
                {
                    AudioSource = new AudioCaptureDevice();
                }
                else
                {
                    AudioSource = new AudioCaptureDevice(info);
                }

                //AudioCaptureDevice source = new AudioCaptureDevice();
                // AudioSource.DesiredFrameSize = 320000;
                // AudioSource.SampleRate = 44100;

                AudioSource.NewFrame += AudioSource_NewFrame;
               AudioSource.Format = SampleFormat.Format16Bit;

                AudioSource.Start();
                int x = 1;
            }
            catch
            {

            }
        

            try
            {
                myConnector = new ConnectorHub.ConnectorHub();
                myConnector.init();
                myConnector.amIvideo = true;
                myConnector.sendReady();
               

                myConnector.startRecordingEvent += MyConnector_startRecordingEvent;
                myConnector.stopRecordingEvent += MyConnector_stopRecordingEvent;
            }
            catch
            {

            }


            this.Closing += MainWindow_Closing;

        }

      

        private void MyConnector_stopRecordingEvent(object sender)
        {
            Dispatcher.Invoke(() =>
            {
                Button_Click_1(null, null);
            });
        }

        private void MyConnector_startRecordingEvent(object sender)
        {
            Dispatcher.Invoke(() =>
            {
                Button_Click(null, null);
            });
        }

        private void AudioSource_NewFrame(object sender, Accord.Audio.NewFrameEventArgs e)
        {
            //byte[] audio = (byte[])e.Signal.RawData.Clone();
            //int mm= AudioSource.BytesReceived;
            if (isRecording)
            {
                try
                {
                    System.TimeSpan diff1 = DateTime.Now.Subtract(sartRecordingTime);
                    if (diff1.Seconds >= 1.0 / 30)
                    {
                        writer.WriteAudioFrame(e.Signal.RawData);
                    }
                        
                }
                catch
                {
                    int x = 0;
                    x++;
                }
                
            }
        }

        private void VideoSource_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                System.Drawing.Image myCurrentImage = (Bitmap)eventArgs.Frame.Clone();


                BitmapImage bi = new BitmapImage();
                bi.BeginInit();

                MemoryStream ms = new MemoryStream();
                myCurrentImage.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                bi.StreamSource = ms;
                bi.EndInit();

                //Using the freeze function to avoid cross thread operations 
                bi.Freeze();
                ImageWebcam.Source = bi;

                

                if(isRecording)
                {
                    captureFunction(eventArgs.Frame);
                }
            });
           
        }


        private void captureFunction(System.Drawing.Bitmap bitmap)
        {
            try
            {
                
                System.TimeSpan diff1 = DateTime.Now.Subtract(sartRecordingTime);
                if(diff1.Seconds>=1.0/30)
                {
                    // vf.WriteVideoFrame(bitmap, diff1);
                    writer.WriteVideoFrame(bitmap,diff1);
                    
                }
                

            }
            catch (Exception e)
            {
                int x = i;

            }
            i++;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isRecording = true;
            
           
            sartRecordingTime = DateTime.Now;
            lastRecordingVideoTime = sartRecordingTime;
            lastRecordingSoundTime = sartRecordingTime;
            string time = DateTime.Now.Hour.ToString();
            time = time + "H" + DateTime.Now.Minute.ToString() + "M" + DateTime.Now.Second.ToString() + "S";
            time = time + ".mp4";
            filename = time;


            int frameRate = 30;
            int bitRate = 400000;
            int audioBitrate = 320000;
            //int audioBitrate = 4096;
             int sampleRate = 44100;
            //int sampleRate = 22050;
            int channels = 1;


           // vf = new VideoFileWriter();
            // vf.Open(time, 640, 480, 30, VideoCodec.MPEG4 );
            writer = new VideoFileWriter();
            writer.Open(filename, 640, 480, frameRate, VideoCodec.Default, bitRate,
                                                AudioCodec.None, audioBitrate, sampleRate, channels);

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            isRecording = false;
           // vf.Close();
            writer.Close();
            try
            {
                myConnector.sendTCPAsync(myConnector.SendFile + filename + myConnector.endSendFile);
            }
            catch (Exception x)
            {
                int i = 0;
                i++;
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            videoSource.SignalToStop();
            myConnector.close();
        }
    }
}
