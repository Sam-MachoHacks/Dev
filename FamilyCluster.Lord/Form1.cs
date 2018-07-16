using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyCluster.Lord
{
    using System.Diagnostics;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string rootFolder = @"Z:\Dev\AkkaTddBootCamp-FamilyCluster\7-RemoteIntoCluster\FamilyCluster\";
        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start($@"{this.rootFolder}FamilyCluster.Brother\bin\Debug\FamilyCluster.Brother.exe");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start($@"{this.rootFolder}FamilyCluster.Sister\bin\Debug\FamilyCluster.Sister.exe");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Process.Start($@"{this.rootFolder}FamilyCluster.FamilyFriend\bin\Debug\FamilyCluster.FamilyFriend.exe");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start($@"{this.rootFolder}Lighthouse\bin\Debug\lighthouse.exe");

        }
    }
}
