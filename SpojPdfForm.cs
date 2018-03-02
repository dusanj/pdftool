
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Reflection;
using System.IO;

namespace SpojPDF
{
    public partial class SpojPdfForm : Form
    {

        private bool[][] stampaj = new bool[1000][];
        private Button btnSave, btnCheck;
        private CheckBox[] chkBox;
        private Form frmPages;
        private int count;

        public SpojPdfForm()
        {
            InitializeComponent();
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (listFiles.SelectedIndex == -1){
                label1.Text = "Prvo izaberite PDF koji treba pomeriti";
            }
            else if (listFiles.SelectedIndex >= 1)
            {
                label1.Text = "";
                int index = listFiles.SelectedIndex;
                string s = (string)listFiles.SelectedItem;
                listFiles.Items.RemoveAt(index);
                listFiles.Items.Insert(index - 1, s);
                listFiles.SelectedIndex = index - 1;
                
                if (stampaj[index] != null || stampaj[index - 1] != null)
                {
                    bool[] stampajTemp = null; 
                    if (stampaj[index] != null) stampajTemp = new bool[stampaj[index].Length];
                    stampajTemp = stampaj[index];
                    stampaj[index] = stampaj[index - 1];
                    stampaj[index - 1] = stampajTemp;
                }

            }
            else label1.Text = "PDF je vec na prvom mestu";
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (listFiles.SelectedIndex == -1)
                label1.Text = "Prvo izaberite PDF koji treba pomeriti";
            else if (listFiles.SelectedIndex != listFiles.Items.Count - 1)
            {
                label1.Text = "";
                int index = listFiles.SelectedIndex;
                string s = (string)listFiles.SelectedItem;
                listFiles.Items.RemoveAt(index);
                listFiles.Items.Insert(index + 1, s);
                listFiles.SelectedIndex = index + 1;
                
                if (stampaj[index] != null || stampaj[index + 1] != null)
                {   
                    bool[] stampajTemp = null;
                    if (stampaj[index] != null) stampajTemp = new bool[stampaj[index].Length];
                    stampajTemp = stampaj[index];
                    stampaj[index] = stampaj[index + 1];
                    stampaj[index + 1] = stampajTemp;
                }
            }
            else label1.Text = "PDF je vec na poslednjem mestu";
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            SaveFileDialog s = new SaveFileDialog();
            s.Filter = "PDF Documents|*.pdf";
            s.AddExtension = true;
            s.DefaultExt = "pdf";
            DialogResult dr = s.ShowDialog(this);

            if (dr != DialogResult.Cancel)
            {
                // Merge the PDFs
                string[] files = new string[listFiles.Items.Count];
                listFiles.Items.CopyTo(files, 0);

                PdfDocument outputDocument = MergeDocuments(files);

                // Save document
                if (outputDocument != null)
                {
                    // Save the document...
                    outputDocument.Save(s.FileName);
                    // ...and start a viewer.
                    Process.Start(s.FileName);
                }
                else 
                    MessageBox.Show("Sile mraka i bezumlja su se umešale u proces spajanja ovog PDFa. Nemoguće je završiti operaciju...",
                    "Greška!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (listFiles.SelectedIndex != -1)
            {
                label1.Text = "";
                int index = listFiles.SelectedIndex;
                listFiles.Items.RemoveAt(index);
                if (stampaj[index] != null) stampaj[index] = null;
                if (index == listFiles.Items.Count)
                {
                    index--;
                }
                if (index >= 0)
                {
                    listFiles.SelectedIndex = index;
                }
            } else label1.Text = "Prvo izaberite PDF koji treba obrisati";
            if (listFiles.Items.Count == 0)
            {
                label1.Text = "";
                btnMoveUp.Enabled = false;
                btnMoveDown.Enabled = false;
                btnRemove.Enabled = false;
                btnMerge.Enabled = false;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            OpenFileDialog d = new OpenFileDialog();
            d.Multiselect = true;
            d.Filter = "PDF Documents|*.pdf";
            DialogResult dr = d.ShowDialog(this);

            if (dr != DialogResult.Cancel && d.FileNames.Length > 0)
            {
                listFiles.Items.AddRange(d.FileNames);

                btnMoveUp.Enabled = true;
                btnMoveDown.Enabled = true;
                btnRemove.Enabled = true;

                if (listFiles.Items.Count > 0)
                {
                    btnMerge.Enabled = true;
                }
            }            
        }
        
        // This function adapted from the sample at http://www.pdfsharp.net/PDFsharp/index.php?option=com_content&task=view&id=52&Itemid=60
        // Document is titled "Concatenate Documents" and is part of the PDFSharp Samples collection.
        /// <summary>
        /// Imports all pages from a list of documents.
        /// </summary>
        public PdfDocument MergeDocuments(string[] files)
        {            
            PdfDocument outputDocument = null;

            ShowWaitingBox waiting = new ShowWaitingBox("Molimo sačekajte...", "U toku je kreiranje PDF dokumenta. Molimo sačekajte... ");
            waiting.Start();

            try
            {
                // Get some file names
                if (files.Length == 0) { return null; }

                // Open the output document
                outputDocument = new PdfDocument();

                int n = 0, p=1;
                // Iterate files 
                foreach (string file in files)
                {
                    if (!File.Exists(file))
                    {
                        waiting.Stop();
                        return null;
                    }
                    // Open the document to import pages from it.
                    PdfDocument inputDocument = CompatiblePdfReader.Open(file, PdfDocumentOpenMode.Import);

                    if (inputDocument == null)
                    {
                        waiting.Stop();
                        return null;
                    }

                    // Iterate pages
                    int count = inputDocument.PageCount;

                    for (int idx = 0; idx < count; idx++)
                    {
                        if (stampaj[n] == null || stampaj[n][idx + 1] == true)
                        {
                            // Get the page from the external document...
                            PdfPage page = inputDocument.Pages[idx];
                            // ...and add it to the output document.
                            outputDocument.AddPage(page);
                            waiting.Update("U toku je kreiranje PDF dokumenta. Molimo sačekajte... " + p++);
                        }
                    }
                    n++;
                }
            }
            catch (Exception e)
            {
                outputDocument = null;
            }

            if (outputDocument == null)
            {
                waiting.Stop();
                return null;
            }   
            waiting.Stop();
            return outputDocument;
        }

        private void listFiles_DragEnter(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void listFiles_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            bool allPDFs = true;
  
            foreach (string s in files)
            {
                if (!s.ToLower().EndsWith(".pdf"))
                {
                    allPDFs = false;
                }
            }

            if (allPDFs && files.Length > 0)
            {
                label1.Text = "";

                listFiles.Items.AddRange(files);

                btnMoveUp.Enabled = true;
                btnMoveDown.Enabled = true;
                btnRemove.Enabled = true;

                if (listFiles.Items.Count > 0)
                {
                    btnMerge.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Mogu da se dodaju samo PDF fajlovi na listu", "File error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private ContextMenuStrip listboxContextMenu;
        
        private void Form1_Load(object sender, EventArgs e)
        {
            //if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            //{
            //    System.Deployment.Application.ApplicationDeployment ad =
            //    System.Deployment.Application.ApplicationDeployment.CurrentDeployment;
            //    this.Text = "PDF Spajac - v" + ad.CurrentVersion;
            //    if (System.Deployment.Application.ApplicationDeployment.CurrentDeployment.IsFirstRun)
            //    {
            //        Info info = new Info();
            //        info.ShowDialog();
            //    }
            //}
            //assign a contextmenustrip
            listboxContextMenu = new ContextMenuStrip();
            listboxContextMenu.Opening += new CancelEventHandler(listboxContextMenu_Opening);
            listFiles.ContextMenuStrip = listboxContextMenu;
            listboxContextMenu.MouseClick += new MouseEventHandler(listboxContextMenu_Click);
            //Info info = new Info();
            //info.ShowDialog();
        }

        private void listFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void listFiles_RightClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                listboxContextMenu.Close();
                int index = this.listFiles.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    listFiles.SelectedIndex=index;
                }
                if (listFiles.SelectedIndex != -1)
                {
                    listboxContextMenu.Show();
                } 
            }

        }

        private void listboxContextMenu_Opening(object sender, CancelEventArgs e)
        {
            //clear the menu and add custom items  
            listboxContextMenu.Items.Clear();
            listboxContextMenu.Items.Add("Izaberite strane \n\r" + listFiles.SelectedItem.ToString()); 
        }

        private void listboxContextMenu_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && listFiles.SelectedItem.ToString().ToLower().EndsWith(".pdf"))
            {
                listboxContextMenu.Close();
                if (File.Exists(listFiles.SelectedItem.ToString()))
                {
                    PdfDocument PDFdoc = CompatiblePdfReader.Open(listFiles.SelectedItem.ToString(), PdfDocumentOpenMode.Import);
                    count = PDFdoc.PageCount;
                    frmPages = new Form();
                    //Set up the form.
                    frmPages.MaximizeBox = false;
                    frmPages.MinimizeBox = false;
                    frmPages.BackColor = Color.White;
                    frmPages.ForeColor = Color.Black;
                    frmPages.Size = new System.Drawing.Size(150, 500);
                    frmPages.AutoScroll = true;
                    frmPages.Text = "Izaberite strane";
                    frmPages.Location = new System.Drawing.Point(5, 5);

                    btnSave = new Button();
                    btnSave.Text = "SACUVAJ";
                    btnSave.AutoSize = true;
                    btnSave.Click += new EventHandler(btnSave_Click);
                    btnSave.Location = new System.Drawing.Point(5,5);
                    frmPages.Controls.Add(btnSave);

                    btnCheck = new Button();                    
                    btnCheck.Text = "Obrni";
                    btnCheck.AutoSize = true;
                    btnCheck.Click += new EventHandler(btnCheck_Click);
                    btnCheck.Location = new System.Drawing.Point(5, 40);
                    frmPages.Controls.Add(btnCheck);

                    chkBox = new CheckBox[count + 1];

                    for (int i = 1; i <= count; i++)
                    {
                        chkBox[i] = new CheckBox();
                        chkBox[i].Text = i.ToString();
                        chkBox[i].Location = new System.Drawing.Point(5, i * 20 + 50);
                        if (stampaj[listFiles.SelectedIndex] != null)
                            chkBox[i].Checked = stampaj[listFiles.SelectedIndex][i];
                        else
                            chkBox[i].Checked = true;
                        frmPages.Controls.Add(chkBox[i]);
                    }
                    frmPages.ShowDialog();
                }
            }
        }
        void btnSave_Click(object sender, EventArgs e)
        {
            stampaj[listFiles.SelectedIndex] = new bool[count + 1];
            for (int i = 1; i <= count; i++)
            {
                stampaj[listFiles.SelectedIndex][i] = chkBox[i].Checked;
            }
            frmPages.Close();
        }
        void btnCheck_Click(object sender, EventArgs e)
        {
            for (int i = 1; i <= count; i++)
            {
                if (chkBox[i].Checked == true) 
                    chkBox[i].Checked = false;
                else
                    chkBox[i].Checked = true;

            }
        }
    }
}
