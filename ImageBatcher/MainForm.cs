using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageBatcher
{
    public partial class MainForm : Form
    {
        private List<string> _imagesPathsList;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowseInput_Click(object sender, EventArgs e)
        {
            if (inputFileDialoge.ShowDialog() != DialogResult.OK)
                return;

            _imagesPathsList = new List<string>(inputFileDialoge.FileNames);
        }

        private void UpdateInputFilesTextBox()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var path in _imagesPathsList)
            {
                stringBuilder.Append(path);
                stringBuilder.Append(';');
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

        }

    }
}
