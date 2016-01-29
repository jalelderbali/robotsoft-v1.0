using System;
using System.Collections.Generic; // securité et perfermonce
using System.ComponentModel; // interface et convertion
using System.Data; // gérer les données
using System.Drawing; // graphique
using System.Linq; //assemblage
using System.Text; //caractére , ASCII code
using System.Windows.Forms; // forme général
using System.Web;
using System.IO; // lecture et ecriture des les entrées et les sortie des données
using System.Threading; //synchronisation
using System.Drawing.Design; //graphique
using System.Drawing.Drawing2D; //graphique
using System.Media; // media 
using System.Net.Sockets; // reception et emission des données
using System.Threading.Tasks; //synchronisation
using System.Speech; // vocal
using System.Speech.Synthesis; // lecture vocal
using System.Speech.Recognition; //reconnaisonce vocal
using System.Diagnostics;
using System.Globalization;
using Enigrobot; // classe personnel
using System.Runtime.InteropServices;
using SlimDX; // SlimDx.dll : lecture et detection des données provenant de l'usb comme  manette analog
using SlimDX.DirectInput; //
using System.Net.NetworkInformation;//
using NativeWifi;
using System.Net;
using System.Drawing.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging;

namespace Enigrobot
{   
    
    public partial class S7 : Form
    {
        // Can be used to notify when the operation completes
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        //initialisation traitement d'image
        private Point backpoint = Point.Empty;
        private bool startProcessing = false;
        //**************************TX/RX initialisation**********************
                              //*************************//
        int picNum = 0;
        Bitmap bmp;
        bool captureimage=false;
         System.Drawing.Image ima = null;
        //
        Boolean Connected = false;
        Socket envSock;
        String S_ArduinoIP = "192.168.1.20";
        int timeout = 0;
        List<byte> Buffer = new List<byte>();
                              //**************************//
        private Bitmap image1 = Properties.Resources.TargetingSystem_Circle_HUD1; // initialisation image1 type bitmap
        SpeechSynthesizer speaker = new SpeechSynthesizer(); // intialisation speaker 

        public S7()
        {
            InitializeComponent();
            label_ButtonList.Text = "0";
            //***************************************************************************************************
            timer1.Enabled = false;
            trackBar4.Value = 90;
        }
//***************************radar *******************************************************
      

//*************************** Partie Connection ************************************************

        //*********************************************
        private void TXtimer_Tick(object sender, EventArgs e)
        {
            //envoyer les enformations en serie
            timeout = timeout + 1;
            if (timeout > 30)
            {
                TXtimer.Stop();
                hist("Déconnecté....");
                timeout = 0;
            }
            try
            {
                byte Auto = 0x00;
                if (Automode.Checked == true)
                {
                    Auto = 0x01;
                }
                else
                {
                    Auto = 0x00;
                }
                
                byte[] envobytes = new Byte[8] 
                { 0x53, 0x44, 0x52, //S,D,R
                (byte)Convert.ToInt32(label_ButtonList.Text), // listes des boutons de 0 à 20
                (byte)Convert.ToInt32(label1.Text),//position du servo selon l'angle de 0 à 180 deg
                (byte)Convert.ToInt32(textBox5.Text), //vitesse des moteur
                Auto,// selection de mode de controle 
                0x00};

                envobytes[7] = (byte)(envobytes[6] + envobytes[5] + envobytes[4] + envobytes[3]);

                envSock.Send(envobytes);


            }
            catch (Exception aux)
            {
                textBox2.Clear();
                hist(aux.ToString());
            }
        }

       
        private void Reception(IAsyncResult res)
        {
            try
            {
                StateObject state = (StateObject)res.AsyncState;
                Socket socket = state.workSocket;
                int numOfBytes = socket.EndReceive(res);
                byte[] temp = new byte[numOfBytes];

                envSock.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(Reception), state);
                Array.Copy(state.Buffer, 0, temp, 0, numOfBytes);
                
                processBytes(temp);

            }

            catch (Exception en)
            {

            }
        }

        private void processBytes(byte[] bytes)
        {

            Buffer.AddRange(bytes);

            int begin = StringLocation(Buffer, "SDR1");

            while (begin != -1 && Buffer.Count - begin > 10) 
            {

                SetText(textBox8, bytes[begin + 5].ToString());//temperature
                SetText(textBox3, bytes[begin + 6].ToString());//gaz
                SetText(textBox6, bytes[begin + 7].ToString());//sonar
                SetText(textBox16, bytes[begin + 8].ToString());//Pitch
                SetText(textBox15, bytes[begin + 9].ToString());//Roll
                //SetText(textBox4, bytes[begin + 10].ToString());//sharp1
                //SetText(textBox7, bytes[begin + 11].ToString());//sharp2
                //SetText(textBox10, bytes[begin + 12].ToString());//batterie
                Buffer.RemoveRange(0, begin + 10);

                begin = StringLocation(Buffer, "SDR1");

                timeout = 0;
            }
        }

        private int StringLocation(List<byte> data, string input)
        {
            int inputSize = input.Length;
            int dataSize = data.Count;
            int compare = dataSize - inputSize + 1;

            char[] inputChar = input.ToCharArray();

            if (dataSize < inputSize)
            {
                return -1;
            }

            for (int i = 0; i < compare; i++)
            {
                for (int j = 0; j < inputSize; j++)
                {
                    if (data[i + j] != inputChar[j])
                    {
                        break;
                    }

                    if (j + 1 == inputSize)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        // disposer chaque text recue a sa position finale
        private static void SetText(TextBox box, string text)
        {
            if (box.InvokeRequired) // 
                box.BeginInvoke(new Action(() => SetText(box, text)));
            else
                box.Text = text;
        }
        private static void SetText(Label box, string text)
        {
            if (box.InvokeRequired) // 
                box.BeginInvoke(new Action(() => SetText(box, text)));
            else
                box.Text = text;
        }
        //intialisation des sockets : taille,type
        public class StateObject
        {
            public Socket workSocket;
            public const int BufferSize = 512;
            public byte[] Buffer = new Byte[BufferSize];
            public StringBuilder sb = new StringBuilder();

        }
        public void hist(string mesg)
        {
            textBox2.Text = textBox2.Text + ">> " + mesg;
        }
       
        // enregisterment des commandes sous l'extention (.eng) qui nous choisie

        private void enregistrerTousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.IO.Stream my_commands = null;
            SaveFileDialog savemyfile = new SaveFileDialog();
            savemyfile.Filter = "txt (*.eng)|*.eng|All files (*.*)|*.*";
            savemyfile.RestoreDirectory = true;
            if ((savemyfile.ShowDialog() == DialogResult.OK))
            {
                my_commands = savemyfile.OpenFile();
                System.IO.StreamWriter my_text = new System.IO.StreamWriter(my_commands);
                my_text.Write(textBox2.Text);
                my_text.Flush();
                my_text.Close();
                my_commands.Close();
            }
        }
        //lire les commandes a partir des fichiers texte
        private void btnGetTemp_Click(object sender, EventArgs e)
        {
            System.IO.Stream my_commands = null;
            string histtext = null;
            OpenFileDialog openmyfile = new OpenFileDialog();
            openmyfile.Filter = "txt (*.txt)|*.txt|All files (*.*)|*.*";
            openmyfile.RestoreDirectory = true;
            if ((openmyfile.ShowDialog() == DialogResult.OK))
            {
                my_commands = openmyfile.OpenFile();
                System.IO.StreamReader my_text = new System.IO.StreamReader(my_commands);
                my_text.ReadLine();
                textBox2.Text = "";
                System.IO.StreamReader r = new System.IO.StreamReader(openmyfile.FileName);
                histtext = r.ReadToEnd();
                textBox2.Text = histtext.ToString();
                my_commands.Close();
            }
        }
  //*************************************************************************************************

  //*********************************Partie Graphique***************************************************
  //Remarque :la partie graphique qui nous avons fait ici en aidant au site web: www.stockoverflow.com//

        protected Graphics graphics;

        private void S7_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            DialogResult dialog = MessageBox.Show("Voulez-vous vraiment fermer le programme?",
                "Exit", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                Application.Exit();
            }
            else
            {
                e.Cancel = true;
            }

        }
       private void button17_Click(object sender, EventArgs e)
        {
            SoundPlayer simpleSound = new SoundPlayer(@"C:\snap.wav");
            simpleSound.Play();
            
        }
        
        private void button19_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button19.BackColor = Color.Red;
            label_ButtonList.Text = "11";
        }

        private void button19_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button19.BackColor = Color.DimGray;
            label_ButtonList.Text = "0";
        }

        private void button6_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button6.BackColor = Color.Red;
            label_ButtonList.Text = "13";
     
        }

        private void button6_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button6.BackColor = Color.DimGray;
            label_ButtonList.Text = "0";
        }

        private void button7_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button7.BackColor = Color.Red;
            label_ButtonList.Text = "12";
         
        }

        private void button7_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button7.BackColor = Color.DimGray;
            label_ButtonList.Text = "0";
        }

        private void button8_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button8.BackColor = Color.Red;
            label_ButtonList.Text = "14";
        }

        private void button8_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            button8.BackColor = Color.DimGray;
            label_ButtonList.Text = "0";
        }
        private void drawarm(Graphics gr)
        {
            const int UpperArmLength = 70;
            const int LowerArmLength = 100;
            const int WristLength = 20;

            gr.SmoothingMode = SmoothingMode.AntiAlias;
            Pen p = new Pen(Color.White, 2);

            float cx = panel10.ClientSize.Width / 2 - 70;
            float cy = panel10.ClientSize.Height / 2-8;
            gr.FillRectangle(Brushes.Black, new Rectangle(5, 136, 100, 34));
            gr.FillEllipse(Brushes.Black, new Rectangle(15, 153, 30, 30));
            gr.DrawEllipse(p, new Rectangle(15, 153, 30, 30));
            gr.FillRectangle(Brushes.Black, new Rectangle(12, 115, 13, 20));
            gr.FillRectangle(Brushes.Black, new Rectangle(8, 105, 20, 10));
            Pen de = new Pen(Color.Black, 23);
            gr.DrawLine(de, 95, 145, 114, 160);
            gr.FillEllipse(Brushes.Black, new Rectangle(76, 153, 30, 30));
            gr.DrawEllipse(p, new Rectangle(76, 153, 30, 30));
            //gr.FillRectangle(Brushes.Black, new Rectangle(90, 57, 12, 6));
            //gr.FillEllipse(Brushes.Black, new Rectangle(65, 68, 16, 16));



            Pen ren = new Pen(Color.Black, 2);
            gr.FillRectangle(Brushes.Black, panel10.Width / 2 - 80, panel10.Height / 2-20 , 20, 40);

            // translation dans le centre du panel
            //Pour chaque étape dans le bras, dessiner puis "prepend" la
            //nouvelle transformation pour représenter le bras suivant dans la séquence.
            gr.TranslateTransform(cx, cy);

            // Rotation de l'épaule
            
            gr.RotateTransform( trackBar3.Value, MatrixOrder.Prepend);
            // Dessiner l'épaule
            gr.FillRectangle(Brushes.Black, 0, -4, UpperArmLength, 15);

            // Translate a la fin de la premiere bras
            gr.TranslateTransform(UpperArmLength, 0, MatrixOrder.Prepend);

            // Rotation coude.
            gr.RotateTransform(trackBar2.Value, MatrixOrder.Prepend);

            // Dess coude rectangle.
            gr.FillRectangle(Brushes.Black, 0, -10, LowerArmLength, 15);
            gr.FillRectangle(Brushes.Black, 75, -35, 25, 20);
            gr.FillRectangle(Brushes.Black, 82, -25, 7, 20);
            // Translate a la fin de la deuxieme bras 
            gr.TranslateTransform(LowerArmLength, 0, MatrixOrder.Prepend);

            // Rotation GRIPPER.
            gr.RotateTransform(-10, MatrixOrder.Prepend);

            // Dess gripper rectangle.
            gr.FillRectangle(Brushes.Black, 0, -1, WristLength, 3);
            //rotation 
            
           
        }
        private void panel10_Paint(object sender, PaintEventArgs e)
        {
            drawarm(e.Graphics);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            panel10.Refresh();
            label57.Text = Convert.ToString(trackBar2.Value);
        }



        private void rotpaint(Graphics f)
        {
            
            f.SmoothingMode = SmoothingMode.AntiAlias;
            Pen pp = new Pen(Color.White, 2);
            f.FillRectangle(Brushes.Black, new Rectangle(10, 30, 80, 40));
            // grande roue
            f.FillRectangle(Brushes.Black, new Rectangle(15, 72, 22, 8));
            f.FillRectangle(Brushes.Black, new Rectangle(15, 20, 22, 8));
            //2eme petite roue
            f.FillRectangle(Brushes.Black, new Rectangle(60, 72, 22, 8));
            f.FillRectangle(Brushes.Black, new Rectangle(60, 20, 22, 8));
            //
            f.DrawRectangle(pp, new Rectangle(15, 45, 15, 15));
            f.FillEllipse(Brushes.Black, new Rectangle(60, 40, 25, 25));
            //
            f.FillRectangle(Brushes.Black, new Rectangle(91, 30, 15, 2));
            f.FillRectangle(Brushes.Black, new Rectangle(91, 68, 15, 2));
            f.FillRectangle(Brushes.Black, new Rectangle(98, 30, 4, 40));
            Pen ten = new Pen(Color.Black, 2);
            float Vx = panel3.ClientSize.Width / 2;
            float Vy = panel3.ClientSize.Height/2 ;
            f.TranslateTransform(Vx, Vy-15);
            f.RotateTransform(trackBar1.Value, MatrixOrder.Prepend);
            f.FillRectangle(Brushes.Black, 0, -8, 53, 16);
            f.DrawLine(ten, 53, 0, 63, -6);
            f.DrawLine(ten, 53, 0, 63, 6);
            f.DrawLine(ten, 63, -6, 72, -2);
            f.DrawLine(ten, 63, 6, 72, 3);
           //f.TranslateTransform(70, 30, MatrixOrder.Prepend); 
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            rotpaint(e.Graphics);
        }

   //************************************Debut Manette CONTROLE***************************************//    
        Joystick joystick;
        JoystickState state = new JoystickState();
        int numPOVs;
        int SliderCount;
        void CreateDevice()
        {
            // make sure that DirectInput has been initialized
            DirectInput dinput = new DirectInput();

            // search for devices
            foreach (DeviceInstance device in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                // create the device
                try
                {
                    joystick = new Joystick(dinput, device.InstanceGuid);
                    joystick.SetCooperativeLevel(this, CooperativeLevel.Exclusive | CooperativeLevel.Foreground);
                    break;
                }
                catch (DirectInputException)
                {
                }
            }

            if (joystick == null)
            {
                MessageBox.Show("Joystick non connecté !!.");
                return;
            }

            foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
            {
                if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                    joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-1000, 1000);

                UpdateControl(deviceObject);
            }

            // acquire the device
            joystick.Acquire();

            // set the timer to go off 12 times a second to read input
            // NOTE: Normally applications would read this much faster.
            // This rate is for demonstration purposes only.
            timer7.Interval = 1000 / 12;
            timer7.Start();
        }

        void ReadImmediateData()
        {
            if (joystick.Acquire().IsFailure)
                return;

            if (joystick.Poll().IsFailure)
                return;

            state = joystick.GetCurrentState();
            if (Result.Last.IsFailure)
                return;

            UpdateUI();
        }
        void ReleaseDevice()
        {
            timer7.Stop();

            if (joystick != null)
            {
                joystick.Unacquire();
                joystick.Dispose();
            }
            joystick = null;
        }

        private void timer7_Tick(object sender, EventArgs e)
        {
            ReadImmediateData();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            ReleaseDevice();
            button27.BackColor = Color.Red;
        }
       
        void UpdateUI()
        {

            string strText = "0";

            label_X.Text = state.X.ToString(CultureInfo.CurrentCulture);
            label_Y.Text = state.Y.ToString(CultureInfo.CurrentCulture);
            label_Z.Text = state.Z.ToString(CultureInfo.CurrentCulture);

            label_XRot.Text = state.RotationX.ToString(CultureInfo.CurrentCulture);
            label_YRot.Text = state.RotationY.ToString(CultureInfo.CurrentCulture);
            label_ZRot.Text = state.RotationZ.ToString(CultureInfo.CurrentCulture);

            int[] slider = state.GetSliders();

            label_S0.Text = slider[0].ToString(CultureInfo.CurrentCulture);
            label_S1.Text = slider[1].ToString(CultureInfo.CurrentCulture);

            int[] pov = state.GetPointOfViewControllers();

            label_P0.Text = pov[0].ToString(CultureInfo.CurrentCulture);
            label_P1.Text = pov[1].ToString(CultureInfo.CurrentCulture);
            label_P2.Text = pov[2].ToString(CultureInfo.CurrentCulture);
            label_P3.Text = pov[3].ToString(CultureInfo.CurrentCulture);

            bool[] buttons = state.GetButtons();

            for (int b = 0; b < buttons.Length; b++)
            {
                if (buttons[b])
                    strText = b.ToString(CultureInfo.CurrentCulture);
            }
            label_ButtonList.Text = strText;

            int yy;
            int xx;
            int zrot;
            int brrot;
            int camrot;
            

            yy = Convert.ToInt32(label_Y.Text);
            xx = Convert.ToInt32(label_X.Text);
            zrot = Convert.ToInt32(label_ZRot.Text);
            brrot = Convert.ToInt32(label_P0.Text);
            camrot = Convert.ToInt32(label_Z.Text);

            if (yy == -1000)
            {
                button19.BackColor = Color.Red;
                label_ButtonList.Text = "11";

            }
            
            else if (yy == 1000)
            {
                button8.BackColor = Color.Red;
                label_ButtonList.Text = "14";

            }
            else
            {
            
                button8.BackColor = Color.DimGray;
                button19.BackColor = Color.DimGray;
            }
            
            if (xx == -1000)
            {
                button7.BackColor = Color.Red;
                label_ButtonList.Text = "12";
            }
            else
                button7.BackColor = Color.DimGray;
            if (xx == 1000 )
            {
                button6.BackColor = Color.Red;
                label_ButtonList.Text = "13";
            }
            else
                button6.BackColor = Color.DimGray;
            if (zrot == -1000)
            {
                if (trackBar2.Value < 160)
                {
                    trackBar2.Value += 2;
                    label_ButtonList.Text = "20";
                    button43.BackColor = Color.Red;
                    label57.Text = Convert.ToString(trackBar2.Value);
                    panel10.Refresh();
                }
            }
            else
                button43.BackColor = Color.White;
            if (zrot == 1000)
            {
                if (trackBar2.Value > 70)
                {
                    trackBar2.Value -= 2;
                    label_ButtonList.Text = "19";
                    button42.BackColor = Color.Red;
                    label57.Text = Convert.ToString(trackBar2.Value);
                    panel10.Refresh();
                }
            }
            else
                button42.BackColor = Color.Lime;

            if (camrot == 1000)
            {
                if (trackBar3.Value < 0)
                {
                    trackBar3.Value += 5;
                    label56.Text = Convert.ToString(trackBar3.Value);
                    button46.BackColor = Color.Red;
                    label_ButtonList.Text = "18";
                    panel10.Refresh();
                }

            }
            else
                button46.BackColor = Color.White;

            if (camrot == -1000)
            {
                if (trackBar3.Value > -160)
                {
                    trackBar3.Value -= 5;
                    label56.Text = Convert.ToString(trackBar3.Value);
                    button39.BackColor = Color.Red;
                    label_ButtonList.Text = "17";
                    panel10.Refresh();
                }
                
            }
            else
                button39.BackColor = Color.Lime;
           
            if (brrot == 9000)
            {
                if (trackBar1.Value < 66)
                {
                    trackBar1.Value += 3;
                    label55.Text = Convert.ToString(trackBar1.Value);
                    panel3.Refresh();
                    label_ButtonList.Text = "16";
                    button45.BackColor = Color.Red;
                }
            }
            else
                button45.BackColor = Color.White;
            if (brrot == 27000)
            {
                if (trackBar1.Value > -66)
                {
                    trackBar1.Value -= 3;
                    label55.Text = Convert.ToString(trackBar1.Value);
                    panel3.Refresh();
                    label_ButtonList.Text = "15";
                    button44.BackColor = Color.Red;
                }
            }
            else
                button44.BackColor = Color.Lime;

            if (strText == "8")
            {
                pictureBox9.Visible = true;
            }
            if (strText == "9")
            {
                pictureBox9.Visible = false;
            }
            
            if (strText == "4")
            {
                if (trackBar4.Value > 0)
                {
                    deg = deg - 3;
                    label1.Text = Convert.ToString(deg);
                    button14.BackColor = Color.Red;
                    trackBar4.Value -= 3;
                }
            }
            else
                button14.BackColor = Color.Lime;
            if (strText == "6")
            {
                button41.BackColor = Color.Red;
            }
            else
                 button41.BackColor = Color.Lime;
            //******************
            if (strText == "5")
            {
                if (trackBar4.Value < 180)
                {
                    deg = deg + 3;
                    label1.Text = Convert.ToString(deg);
                    button15.BackColor = Color.Red;
                    trackBar4.Value += 3;
                }
            }
            else
            {
                button15.BackColor = Color.Lime;
            }
            //***********************
            if (strText == "7")
            {
                button40.BackColor = Color.Red;
            }
            else
            {
                button40.BackColor = Color.Lime;
            }
           
            //*****************************
            if (strText == "1")
            {
                textBox5.Text = "100";
            }
            //********************************
            if (strText == "2")
            {
                textBox5.Text = "200";
            }
            //********************************
            if (strText == "3")
            {
                textBox5.Text = "255";
            }
        }

        void UpdateControl(DeviceObjectInstance d)
        {
            if (ObjectGuid.XAxis == d.ObjectTypeGuid)
            {
                label_XAxis.Enabled = true;
                label_X.Enabled = true;
            }
            if (ObjectGuid.YAxis == d.ObjectTypeGuid)
            {
                label_YAxis.Enabled = true;
                label_Y.Enabled = true;
            }
            if (ObjectGuid.ZAxis == d.ObjectTypeGuid)
            {
                label_ZAxis.Enabled = true;
                label_Z.Enabled = true;
            }
            if (ObjectGuid.RotationalXAxis == d.ObjectTypeGuid)
            {
                label_XRotation.Enabled = true;
                label_XRot.Enabled = true;
            }
            if (ObjectGuid.RotationalYAxis == d.ObjectTypeGuid)
            {
                label_YRotation.Enabled = true;
                label_YRot.Enabled = true;
            }
            if (ObjectGuid.RotationalZAxis == d.ObjectTypeGuid)
            {
                label_ZRotation.Enabled = true;
                label_ZRot.Enabled = true;
            }

            if (ObjectGuid.Slider == d.ObjectTypeGuid)
            {
                switch (SliderCount++)
                {
                    case 0:
                        label_Slider0.Enabled = true;
                        label_S0.Enabled = true;
                        break;

                    case 1:
                        label_Slider1.Enabled = true;
                        label_S1.Enabled = true;
                        break;
                }
            }

            if (ObjectGuid.PovController == d.ObjectTypeGuid)
            {
                switch (numPOVs++)
                {
                    case 0:
                        label_POV0.Enabled = true;
                        label_P0.Enabled = true;
                        break;

                    case 1:
                        label_POV1.Enabled = true;
                        label_P1.Enabled = true;
                        break;

                    case 2:
                        label_POV2.Enabled = true;
                        label_P2.Enabled = true;
                        break;

                    case 3:
                        label_POV3.Enabled = true;
                        label_P3.Enabled = true;
                        break;
                }
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            if (joystick == null)
            {
                CreateDevice();
            }
            else
                ReleaseDevice();

            UpdateUI();
            
        }
//**************************************fin joystick controle*******************************//



        

        private void button18_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            speech(); 
        }
        private void speech()
        {
            speaker.Rate = Convert.ToInt32(-1);
            speaker.Volume = Convert.ToInt32(100);
            speaker.SpeakAsync("Temperature : " + textBox8.Text + " degree ");
            speaker.SpeakAsync( " Gas Concentration : " + textBox3.Text + " ppm " );
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            speech();
        }


       

       
        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            foreach (RecognizedWordUnit words in e.Result.Words)
            {
                switch (words.Text)
                {
                    case "left":
                        {
                            button7.BackColor = Color.Red;
                            button6.BackColor = Color.Blue;
                            button19.BackColor = Color.Blue;
                            button8.BackColor = Color.Blue;
                            button26.BackColor = Color.Blue;
                        }
                        break;
                    case "right":
                        {
                            button6.BackColor = Color.Red;
                            button7.BackColor = Color.Blue;
                            button19.BackColor = Color.Blue;
                            button8.BackColor = Color.Blue;
                            button26.BackColor = Color.Blue;
                        }
                        break;
                    case "forword":
                        {
                            button19.BackColor = Color.Red;
                            button7.BackColor = Color.Blue;
                            button6.BackColor = Color.Blue;
                            button8.BackColor = Color.Blue;
                            button26.BackColor = Color.Blue;
                        }
                        break;
                    case "backword":
                        {
                            button8.BackColor = Color.Red;
                            button7.BackColor = Color.Blue;
                            button6.BackColor = Color.Blue;
                            button19.BackColor = Color.Blue;
                            button26.BackColor = Color.Blue;
                        }
                        break;
                    case "stop":
                        {
                            button8.BackColor = Color.Blue;
                            button7.BackColor = Color.Blue;
                            button6.BackColor = Color.Blue;
                            button19.BackColor = Color.Blue;
                            button26.BackColor = Color.Red;
                        }
                        break;
                    case "talk":
                        {
                            speech(); 
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        
        private void button31_Click(object sender, EventArgs e)
        {
                button31.BackColor = Color.Green;
                SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine();
                recognizer.SetInputToDefaultAudioDevice();
                Choices choice = new Choices("left", "right", "forword", "backword","stop","talk");
                GrammarBuilder gb = new GrammarBuilder(choice);
                Grammar g = new Grammar(gb);
                recognizer.LoadGrammar(g);
                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
                recognizer.RecognizeAsync(RecognizeMode.Multiple);


        }
       

        
       
        private void button5_Click(object sender, EventArgs e)
        {
            SoundPlayer simpleSound = new SoundPlayer(@"C:\snap.wav");
            simpleSound.Play();
            ima = (System.Drawing.Image)bmp.Clone();
            captureimage = true;
            if (captureimage)
            {
                string filename;
                Directory.CreateDirectory(@"C:\eBox2300");
                filename = (@"C:\rescue robot\" + "image_" + picNum + ".jpeg");
                ima.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                picNum += 1;
                captureimage = false;
            }
            
        }

        private void chargerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog opendial = new OpenFileDialog();
            opendial.Filter = "Command file(.eng) | *.eng";
            if (opendial.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
            }
        }

        private void manuelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("Manuel robotsoft.docx");
        }
        
        private void nouveauToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process p = null;
            try
            {
                p = new Process();
                p.StartInfo.FileName = "notepad";
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Erreur fatale !!");
            }
        }

        private void sortirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void siteWebToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WebBrowser wb = new WebBrowser();
            wb.Show();
        }

       

        
        int medval;
        private void panel8_Paint(object sender, PaintEventArgs e)
        {
            
            medval = Convert.ToInt32(textBox6.Text);
            int WIDTH = panel8.Width;
            int HEIGHT = panel8.Height;
            Graphics g = e.Graphics;
            Pen p = new Pen(Color.White, 1f);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawEllipse(p, -100, HEIGHT / 2 - 100, 200, 200);

            g.DrawEllipse(p,-20,HEIGHT/2 -20, 40, 40);
            g.DrawEllipse(p,-40,HEIGHT/2 -40, 80, 80);
            g.DrawEllipse(p,-60,HEIGHT/2 -60, 120, 120);
            g.DrawEllipse(p,-80,HEIGHT/2 -80, 160, 160);
            g.DrawEllipse(p, -100,HEIGHT/2 -100, 200, 200);
            g.DrawEllipse(p, -108, HEIGHT / 2 - 108, 216, 216);
            g.DrawLine(p, new Point(0, HEIGHT/2), new Point(WIDTH, HEIGHT/2)); // 
     
            //%%%%%%%%%%%%%%%%%%%%%%%%
           

        }

     
    #region Dessiner les point envoyer par les capteurs distance
        Bitmap distanceBMP = new Bitmap(70, 20);
        Point origin = new Point(70 / 2, 60 - 1);
        private void DrawPoint(int r, int degrees, int light)
        {
            degrees = trackBar4.Value;
            Pen redPen = new Pen(Color.Red, 1);
            double x = r * Math.Cos((double)degrees * Math.PI / 180);
            double y = r * Math.Sin((double)degrees * Math.PI / 180);
            Color lightColor = new Color();
            lightColor = Color.FromArgb((light) % 255, (light) % 255, (light) % 255);
            Pen lightPen = new Pen(Color.Red, 7);
            /* Dessiner les lignes entre les points */
            using (var graphics = Graphics.FromImage(distanceBMP))
            {

                graphics.DrawLine(lightPen, (int)origin.X, (int)origin.Y, (int)x + origin.X, (int)-y + origin.Y);

            }
            ///*  Draw the points   */
            //for (int i = 0; i < 10; i++)
            //    for (int j = 0; j < 10; j++)
            //         distanceBMP.SetPixel((int)x + origin.X - i, (int)-y + origin.Y - j, Color.Red);
            //         panel8.BackgroundImage = null;
            //         panel8.BackgroundImage = distanceBMP;
        }
        #endregion

        private void informationToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            inf f1 = new inf();
            f1.Show();
        }
       
 //********************************Camera**************************************************************



        private void button45_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar1.Value < 66)
            {
                trackBar1.Value += 3;
                label55.Text = Convert.ToString(trackBar1.Value) + "deg";
                label_ButtonList.Text = "16";
                panel3.Refresh();
            }
           

        }

        private void button45_MouseUp(object sender, MouseEventArgs e)
        {
            button45.BackColor = Color.White;
            label_ButtonList.Text = "0";
        }

        private void button44_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar1.Value > -66)
            {
                trackBar1.Value -= 3;
                label55.Text = Convert.ToString(trackBar1.Value) + "deg";
                button44.BackColor = Color.Red;
                label_ButtonList.Text = "15";
                panel3.Refresh();
            }
            
        }

        private void button44_MouseUp(object sender, MouseEventArgs e)
        {
            button44.BackColor = Color.Lime;
            label_ButtonList.Text = "0";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Clear();
        }
        //Ultrason capteur
        SolidBrush brush = new SolidBrush(Color.White);
        SolidBrush brush1 = new SolidBrush(Color.Black);

        public void drawLine(int distance, int degree)
        {
            Graphics g = Graphics.FromHwnd(panel8.Handle);
            var halfY = panel8.ClientRectangle.Height / 2;
            Pen pen = new Pen(brush, 8);
            g.DrawLine(pen, Convert.ToInt32((distance * Math.Sin((double)degree * 0.0174532925))), panel8.Height - Convert.ToInt32(halfY + distance * Math.Cos((double)degree * 0.0174532925)), 0, halfY);
        }
        public void drawPoint(int x, int y)
        {
            Graphics g = Graphics.FromHwnd(panel8.Handle);
            Point dPoint = new Point(x, (panel8.Height - y));
            dPoint.X = dPoint.X - 1;
            dPoint.Y = dPoint.Y - 1;
            Rectangle rect = new Rectangle(dPoint, new Size(5, 5));
           
            g.FillRectangle(brush1, rect);
            g.Dispose();
        }
        int degree = 90;
        int distance;
        Boolean dots = false;
        Boolean diffrence = false;
        int[] positionArray = new int[280];
        private void timer2_Tick_1(object sender, EventArgs e)
        {
            var halfX = panel8.ClientRectangle.Width / 2;
            var halfY = panel8.ClientRectangle.Height / 2;
            int xposition = Convert.ToInt32((distance * Math.Sin((double)degree * 0.0174532925)));
            int yposition = Convert.ToInt32(halfY + distance * Math.Cos((double)degree * 0.0174532925));
            degree = trackBar4.Value;

            if (diffrence == true)
            {
                if (positionArray[degree] == 0)
                {
                    brush.Color = Color.LimeGreen;
                }
                else if ((positionArray[degree] - distance) >= 20 || (positionArray[degree] - distance) <= -20)
                {
                    brush.Color = Color.Red;
                }
            }
            positionArray[degree] = distance;
            drawPoint(xposition, yposition);
            if (dots == false)
            {
               drawLine(distance, degree);
            }
            if (degree == 0 || degree == 180)
            {
                Array.Clear(positionArray, 0, positionArray.Length);
                brush.Color = Color.White;
                panel8.Refresh();
            }

        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                panel8.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            timer2.Start();
        }

        
        private void button12_Click(object sender, EventArgs e)
        {
            panel8.Refresh();
            Array.Clear(positionArray, 0, positionArray.Length);
            brush.Color = Color.White;
        }


        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close(); 
        }
        
        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //connexion vers l'arduino
           textBox2.Clear();
            try
            {

                // premiere adresse IP associer au localhost  
                System.Net.IPAddress ipAddr = System.Net.IPAddress.Parse(S_ArduinoIP);

                // Creation network "endpoint"  
                System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(ipAddr, 5050);

                // Creation d'un socket tcp connexion 
                envSock = new Socket(
                    AddressFamily.InterNetwork,// Specifier le type d'adressage 
                    SocketType.Stream,   // type de socket   
                    ProtocolType.Tcp     // Specification de protocol   
                    );

                envSock.NoDelay = false;   // 

                // Establissemet de connection au  host  
                envSock.Connect(ipEndPoint);
                StateObject state = new StateObject();
                state.workSocket = envSock;
                envSock.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(Reception), state);

                hist("Connecté.....");
                Connected = true;
                //toolStripProgressBar1.Value = 100;
                TXtimer.Start();
            }
            catch (Exception eg)
            {
                hist(Convert.ToString(eg));
                toolStripProgressBar1.Value = 0;
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
           
        }
       
        private void button46_MouseDown(object sender, MouseEventArgs e)
        {
            if ( trackBar3.Value <0)
            {
                trackBar3.Value += 10;
                label_ButtonList.Text = "18";
                label56.Text = Convert.ToString(trackBar3.Value);
                panel10.Refresh();
            }
            
        }

        private void button46_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
        }

        private void button39_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar3.Value > -160)
            {
                trackBar3.Value -= 10;
                label_ButtonList.Text = "17";
                label56.Text = Convert.ToString(trackBar3.Value);
                panel10.Refresh();
            } 
                
        }

        private void button39_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
        }
        private void button9_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            horizon1.Pitch = Convert.ToInt32(textBox15.Text);
        }

        private void textBox16_TextChanged(object sender, EventArgs e)
        {
            if(Convert.ToInt32(textBox16.Text) < 90 && Convert.ToInt32(textBox16.Text) > -90 )
            horizon1.Roll = Convert.ToInt32(textBox16.Text);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            panel10.Refresh();
            label56.Text = Convert.ToString(trackBar3.Value);
        }

       

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            c2DPushGraph3.AddLine("Gaz", Color.Yellow);
            c2DPushGraph3.Push(Convert.ToInt32(textBox3.Text), "Gaz");
            c2DPushGraph3.UpdateGraph();
        }
        int deg;
        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            degree = deg;
        }

        private void button15_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar4.Value < 180)
            {
                deg = deg + 3;
                label1.Text = Convert.ToString(deg);
                button15.BackColor = Color.Red;
                trackBar4.Value += 3;
            }
        }

        private void button14_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar4.Value > 0)
            {
                deg = deg - 3;
                label1.Text = Convert.ToString(deg);
                
                button14.BackColor = Color.Red;
                trackBar4.Value -= 3;
            }
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            distance = Convert.ToInt32(label1.Text);
        }

        private void button43_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar2.Value < 160)
            {
                trackBar2.Value += 3;
                label_ButtonList.Text = "20";
                label57.Text = Convert.ToString(trackBar2.Value);
            }
            panel10.Refresh();
        }

        private void button42_MouseDown(object sender, MouseEventArgs e)
        {
            if (trackBar2.Value >70)
            {
                trackBar2.Value -= 3;
                label_ButtonList.Text = "19";
                label57.Text = Convert.ToString(trackBar2.Value);
            }
            panel10.Refresh();
        }

        private void button43_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
        }

        private void button42_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
        }

        private void button15_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
            button15.BackColor = Color.Lime;
            panel8.Refresh();
            
        }

        private void button14_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
            button14.BackColor = Color.Lime;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            progressBar2.Value = Convert.ToInt32(textBox5.Text);
        }

        

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            c2DPushGraph4.AddLine("IR Line1", Color.White);
            c2DPushGraph4.Push(Convert.ToInt32(textBox7.Text), "IR Line1");
            c2DPushGraph4.UpdateGraph();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            //
            c2DPushGraph5.AddLine("IR Line2", Color.White);
            c2DPushGraph5.Push(Convert.ToInt32(textBox4.Text) * (-1), "IR Line2");
            c2DPushGraph5.UpdateGraph();
        }

        private void label_ButtonList_TextChanged(object sender, EventArgs e)
        {
            hist(Convert.ToString(label_ButtonList.Text));
        }

        private void startToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            recoitframe();
            timer3.Enabled = true;
        }

        private void recoitframe()
        {
            textBox2.Clear();
            
            string sourceURL = "http://192.168.1.110/image/jpeg.cgi";
            byte[] buffer = new byte[100000];
            int read, total = 0;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(sourceURL);
                req.Credentials = new NetworkCredential("admin", "robotexp");
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();
                while ((read = stream.Read(buffer, total, 1000)) != 0)
                {
                    total += read;
                }
                //traitement d'image
                bmp = (Bitmap)Bitmap.FromStream(new MemoryStream(buffer, 0, total));
                ResizeNearestNeighbor filter2 = new ResizeNearestNeighbor(bmp.Width, bmp.Height);
                bmp = filter2.Apply(bmp);
                if (startProcessing)
                {
                    traitementimage(ref bmp);
                }                          
                
                panel2.BackgroundImage = bmp;
                toolStripProgressBar2.Value = 100;
            }
            catch (Exception)
            {
                hist("Impossible de se connecter au caméra !!!");
                toolStripProgressBar2.Value = 0;
            }
            
        }
        private void traitementimage(ref Bitmap image)
        {

            ColorFiltering filter = new ColorFiltering();
            filter.Red = new IntRange(254, 255);
            filter.Green = new IntRange(0, 240);
            filter.Blue = new IntRange(0, 240);
            Bitmap tmp = filter.Apply(image);
            IFilter grayscale = new GrayscaleBT709();
            tmp = grayscale.Apply(tmp);
            BitmapData bitmapData = tmp.LockBits(new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(bitmapData);
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
            Rectangle[] rects = blobCounter.GetObjectsRectangles();
            tmp.UnlockBits(bitmapData);
            tmp.Dispose();
            if (rects.Length != 0)
            {
                backpoint = Centre(rects[0]);
                lignes(ref image, backpoint);

            }

        }


        private Point Centre(Rectangle rect)
        {
            int x = rect.X + (rect.Width / 2);
            int y = rect.Y + (rect.Height / 2);
            Point p = new Point(x, y);

            return p;
        }


        public void lignes(ref Bitmap image, Point p)
        {
            // ligne horizontale
            Graphics g = Graphics.FromImage(image);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Pen p1 = new Pen(Color.Red, 1);
            Pen p2 = new Pen(Color.Red, 1);
            Point ph = new Point(image.Width, p.Y);
            Point ph2 = new Point(0, p.Y);

            g.DrawLine(p2, p, ph);
            g.DrawLine(p2, p, ph2);
            // ligne verticale
            ph = new Point(p.X, 0);
            ph2 = new Point(p.X, image.Height);
            g.DrawLine(p1, p, ph);
            g.DrawLine(p1, p, ph2);
            //rectangles
            g.DrawRectangle(p1, p.X - 20, ph2.Y / 2 - 50, 40, 40);
            g.DrawRectangle(p1, p.X - 25, ph2.Y / 2 - 55, 50, 50);
            //
            FontFamily fam = new FontFamily("Microsoft Sans Serif");
            Font font = new System.Drawing.Font(fam, 8, FontStyle.Bold);
            string rand = "ISERR " + Convert.ToString(p.X);
            g.DrawString(rand, font, Brushes.Red, new Point(p.X - 50, ph2.Y / 2 - 70));
            g.Dispose();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startProcessing = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startProcessing = false;
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            recoitframe();
        }

        private void signalStrength1_Load(object sender, EventArgs e)
        {
            WlanClient client = new WlanClient();

            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    Wlan.Dot11Ssid ssid = network.dot11Ssid;
                    string networkName = Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
                    if (networkName == "linksys")
                    {
                        toolStripStatusLabel7.Text = networkName;
                        toolStripProgressBar3.Value = Convert.ToInt32(network.wlanSignalQuality);
                        toolStripStatusLabel5.Text = Convert.ToString(toolStripProgressBar3.Value);
                        signalStrength1.Value = Convert.ToInt32(network.wlanSignalQuality);
                    }

                }
            }
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            c2DPushGraph2.AddLine("temperature", Color.Red);
            c2DPushGraph2.Push(Convert.ToInt32(textBox8.Text), "temperature");
            c2DPushGraph2.UpdateGraph();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            label1.Text = "0";
            trackBar4.Value = 0;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            label1.Text = "90";
            trackBar4.Value = 90;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            label1.Text = "180";
            trackBar4.Value = 180;
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            if (Convert.ToInt32(textBox15.Text) < 90 && Convert.ToInt32(textBox15.Text) > -90)
                horizon1.Roll = Convert.ToInt32(textBox15.Text);
        }

        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            label1.Text = Convert.ToString(trackBar4.Value);
        }

        private void button41_MouseDown(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "6";
            button41.BackColor = Color.Red;
        }

        private void button41_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
            button41.BackColor = Color.Lime;
        }

        private void button40_MouseDown(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "7";
            button40.BackColor = Color.Red;
        }

        private void button40_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
            button40.BackColor = Color.Blue ;
        }

        private void button4_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox9.Visible = true;
            label_ButtonList.Text = "30";
        }

        private void button4_MouseUp(object sender, MouseEventArgs e)
        {
            label_ButtonList.Text = "0";
        }

        private void button11_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox9.Visible = false;
            label_ButtonList.Text = "31";
        }

        private void button11_MouseUp(object sender, MouseEventArgs e)
        {
            
            label_ButtonList.Text = "0";
        }

       
      

      


       
       

       


          
    }
}
  

