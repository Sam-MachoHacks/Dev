using System;
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

        private string rootFolder = @"Z:\Dev\demo\";

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