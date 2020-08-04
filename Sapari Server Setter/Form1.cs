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
using System.Text.RegularExpressions;

namespace Sapari_Server_Setter {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }


        private string serverLink;
        private string chosenDirectory;


        private void ShowMessageBoxNotification(string title, string contents) {
            MessageBoxButtons b = MessageBoxButtons.OK;
            MessageBox.Show(contents, title, b);
        }


        private bool IsValidWRLFile(string path) {
            // Checks if the given .wrl file contains a valid Sony_WorldInfo { ... } entry.
            string[] lines = File.ReadAllLines(path);
            foreach (string l in lines) {
                if (l.Contains("Sony_WorldInfo {")) {
                    return true;
                }
            }
            return false;
        }


        private List<string> GetWRLFiles(string dir) {
            // Recursively fetches all the relevant .wrl files in the directory and subdirectories.
            List<string> filesInDirectory = Directory.GetFiles(dir).Where(x => x.EndsWith(".wrl")).ToList();
            string[] foldersInDirectory = Directory.GetDirectories(dir);
            if (foldersInDirectory.Length <= 0) {
                // The base case; No subdirectories. Return the files in the directory.
                return filesInDirectory;
            }
            else {
                foreach (string d in foldersInDirectory) {
                    // Iterate over the folders found in the directory
                    filesInDirectory.AddRange(GetWRLFiles(d));
                }
                return filesInDirectory;
            }
        }


        private bool WriteLinkToFile(string file) {
            // Write the server IP/web address to the file under the Sony_WorldInfo entry.
            List<string> lines = File.ReadAllLines(file).ToList();

            string lineToWrite = $"\tcpBureau\t\"{serverLink}\"";

            Regex WLSMatchPattern = new Regex("^\\s*cpBureauWLS\\s*\"vrml\\.sony\\.co\\.jp:.*\"");
            Regex normalMatchPattern = new Regex("^\\s*cpBureau\\s*\".*\"");

            bool foundRegexMatch = false;

            // Just find and replace all entries of CPBureauWLS and CPBureau with lineToWrite, then do duplicate cleanup afterward...
            for (int i = 0; i < lines.Count; i++) {
                if (WLSMatchPattern.Match(lines[i]).Success) {
                    lines[i] = lineToWrite;
                    foundRegexMatch = true;
                }
                else if (normalMatchPattern.Match(lines[i]).Success) {
                    lines[i] = lineToWrite;
                    foundRegexMatch = true;
                }
            }
            if (foundRegexMatch == false) {
                ShowMessageBoxNotification("Write Error", $"Couldn't find relevant cpBureau or cpBureauWLS entry in file {file}!");
                return false;
            }

            bool firstMatchFound = false;
            for (int i = 0; i < lines.Count; i++) {
                // Find the indices of the duplicates and mark them to be filtered...
                if (normalMatchPattern.Match(lines[i]).Success) {
                    if (firstMatchFound == false) {
                        firstMatchFound = true;
                    }
                    else {
                        lines[i] = "!!!FilterMe";
                    }
                }
            }

            try {
                File.WriteAllLines(file, lines.Where(x => x.Contains("!!!FilterMe") == false).ToArray());
            }
            catch {
                ShowMessageBoxNotification("Write Error", $"Error writing to file {file}.\n\n Make sure the file isn't set to read-only or presently in use, and make sure you're running the application as administrator!");
                return false;
            }
            return true;
        }


        private bool IsValidGameDirectory(string dir) {
            // Check if the chosen directory is a valid game directory...
            if (dir.EndsWith("Community Place Browser") == false) {
                return false;
            }
            string[] subdirectories = Directory.GetDirectories(dir);
            bool foundWorldFolder = false;
            foreach (string sd in subdirectories) {
                if (sd.EndsWith("world")) {
                    foundWorldFolder = true;
                }
            }
            return foundWorldFolder;
        }


        private void SetIPButton_Click(object sender, EventArgs e) {
            string currDir = chosenDirectory;
            string invalidDirMessage = "Invalid game directory. Folder must be named \"Community Place Browser\" and contain a \"world\" folder within.";
            if (chosenDirectory == null || currDir.Length <= 0) {
                ShowMessageBoxNotification("Error", invalidDirMessage);
                return;
            }
            if (currDir.Length > 0 && IsValidGameDirectory(currDir) == false) {
                ShowMessageBoxNotification("Error", invalidDirMessage);
                return;
            }
            List<string> files = GetWRLFiles(currDir).Where(x => IsValidWRLFile(x)).ToList();
            if (files.Count <= 0) {
                ShowMessageBoxNotification("Write Error", "Couldn't find any valid .wrl files to write to!");
                return;
            }
            foreach (string f in files) {
                bool successfulWrite = WriteLinkToFile(f);
                if (!successfulWrite) {
                    ShowMessageBoxNotification("Warning", "Couldn't write to all files.");
                    return;
                }
            }
            ShowMessageBoxNotification("Success", "Server set successfully!");
        }


        private void serverNameTextBox_TextChanged(object sender, EventArgs e) {
            serverLink = serverNameTextBox.Text;
        }


        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            string s = "Sapari Community Place Server Setter 1.0\nCopyright © 2020\nLicensed under the MIT license.\nCreated by Hxdce";
            ShowMessageBoxNotification("About", s);
        }


        private void GameDirectoryTextBox_TextChanged(object sender, EventArgs e) {
            chosenDirectory = GameDirectoryTextBox.Text;
        }


        private void FolderBrowserButton_Click(object sender, EventArgs e) {
            FolderBrowserDialog d1 = new FolderBrowserDialog();
            DialogResult r = d1.ShowDialog();
            if (r == DialogResult.OK) {
                chosenDirectory = d1.SelectedPath;
                GameDirectoryTextBox.Text = d1.SelectedPath;
            }
        }
    }
}
