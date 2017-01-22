using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using FTD2XX_NET;

namespace BOOTL
{
    public partial class Form1 : Form
    {
        FileStream plik;
        string linia, rozszerzenie;
        int liczba;
        byte[] program = new byte[200000];
        FTDI ft = new FTDI();
        uint ilosc_bajtow, ilosc_urzadzen, bajtow_zapisanych;
        FTDI.FT_DEVICE_INFO_NODE[] Lista_urzadzen;

        public Form1()
        {
            InitializeComponent();
        }


        //ładowanie hexa
        private void button1_Click(object sender, EventArgs e)
        {
            ilosc_bajtow = 0;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                plik = new FileStream(openFileDialog1.FileName, FileMode.Open);
                rozszerzenie = Path.GetExtension(openFileDialog1.FileName);

                StreamReader odczyt = new StreamReader(plik);
                
                if (rozszerzenie == ".bin")
                {
                    //binarka = odczyt.ReadToEnd();
                    //memoBox.AppendText(binarka.Substring(0, 16));
                    //for(int i=0; i<binarka.Length; i++)
                    //program = binarka.ToCharArray();
                    //na razie nie ma obslugi plikow *.bin
                }

                else if (rozszerzenie == ".hex")
                {    
                    do
                    {
                        linia = odczyt.ReadLine();
                        if (linia != null)
                        {
                            if (linia.Substring(7, 2) != "00") continue;

                            liczba = Convert.ToInt16(linia.Substring(1, 2), 16);
                            for (int i = 0; i < liczba; i++)
                            {
                                program[ilosc_bajtow++] = Convert.ToByte(linia.Substring(9 + i * 2, 2), 16);
                            }
                            
                          }

                    } while (linia != null);
                }
                odczyt.Close();
                plik.Close();
                memoBox.AppendText("Ilość bajtów: " + Convert.ToString(ilosc_bajtow) + ".\r\n");
            }
        }


        //wyszukiwanie urządzeń ftdi
        private void button2_Click(object sender, EventArgs e)
        {
            ft.GetNumberOfDevices(ref ilosc_urzadzen);
            
            Lista_urzadzen = new FTDI.FT_DEVICE_INFO_NODE[ilosc_urzadzen];
            
            ft.GetDeviceList(Lista_urzadzen);

            listBox1.Items.Clear();
            for (int i = 0; i < ilosc_urzadzen; i++)
            {
                listBox1.Items.Add(Convert.ToString(Lista_urzadzen[i].Description));
            }

            memoBox.AppendText("Wybierz urządzenie.\r\n");
        }


        //uruchomienie bootloadera
        private void button3_Click(object sender, EventArgs e)
        {
            byte[] ramka_odbioru = new byte[1000];
            uint odczytano_bajtow = 0;
            uint rozmiar_strony = 0;
            byte[] ramka_wyslania = new byte[1280];
            uint count = 0;

            ft.OpenBySerialNumber(Lista_urzadzen[listBox1.SelectedIndex].SerialNumber);
            ft.SetBaudRate(9600);
            ft.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);

            ft.SetRTS(true);
            
            //System.Threading.Thread.Sleep(100);
            
            ft.SetRTS(false);
            
            do
            {
                ft.Read(ramka_odbioru, 1, ref odczytano_bajtow);
            } while (ramka_odbioru[0] != '?');

            ft.Write("ui", 2, ref bajtow_zapisanych);

            ft.SetTimeouts(500, 0);
            ft.Read(ramka_odbioru, 36, ref odczytano_bajtow);

           
            string[] dane = new string[5];
            string temp;
            temp = ASCIIEncoding.ASCII.GetString(ramka_odbioru);
            dane = temp.Split(',');

           // int rozmiar_strony = 0;
            rozmiar_strony = Convert.ToUInt16(dane[0].Substring(3));

            ft.Write("w", 1, ref bajtow_zapisanych);

            count = (ilosc_bajtow / rozmiar_strony) + 1;

            for (uint i = ilosc_bajtow; i < rozmiar_strony * count; i++)
            {
                program[i] = 0x55;
            }

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < rozmiar_strony; j++)
                    ramka_wyslania[j] = program[rozmiar_strony * i + j];
                do
                {
                    ft.Read(ramka_odbioru, 1, ref odczytano_bajtow);
                } while (ramka_odbioru[0] != '@');

                ft.Write("1", 1, ref bajtow_zapisanych);
                ft.Write(ramka_wyslania, rozmiar_strony, ref bajtow_zapisanych);
            }

            memoBox.AppendText("Wgrano poprawnie program!\r\n");

            ft.Close();
        }


        //sprawdzanie połączenia
        private void button4_Click(object sender, EventArgs e)
        {
            byte[] ramka_odbioru = new byte[100];
            uint odczytano_bajtow = 0;

            ft.OpenBySerialNumber(Lista_urzadzen[listBox1.SelectedIndex].SerialNumber);
            ft.SetBaudRate(9600);
            ft.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);

            //System.Threading.Thread.Sleep(1000);
            ft.SetRTS(true);
           
            ft.SetRTS(false);

            do
            {
                ft.Read(ramka_odbioru, 1, ref odczytano_bajtow);
            } while (ramka_odbioru[0] != '?');

            ft.Write("ui", 2, ref odczytano_bajtow);

            //ft.Read(
            ft.SetTimeouts(500, 0);
            ft.Read(ramka_odbioru, 36, ref odczytano_bajtow);

            ft.Write("e", 1, ref odczytano_bajtow);

            ft.Close();
            string[] dane = new string[5];
            string temp;
            temp = ASCIIEncoding.ASCII.GetString(ramka_odbioru);
            dane = temp.Split(',');
           
            UInt16 rozmiar_strony = 0;
            rozmiar_strony = Convert.ToUInt16(dane[0].Substring(3));
            
            memoBox.AppendText("\r\nRozmiar strony: " + rozmiar_strony.ToString() + "\r\n");
            //memoBox.AppendText("\r\nRozmiar strony: " + dane[0] + "\r\n");
            memoBox.AppendText("Coś tam: " + dane[1] + "\r\n");
            memoBox.AppendText("Procesor: " + dane[2] + "\r\n");
            memoBox.AppendText("Częstotliwość taktowania: " + dane[3] + "\r\n");
            memoBox.AppendText("Wersja bootloadera: " + dane[4] + "\r\n");
        }

    }
}
