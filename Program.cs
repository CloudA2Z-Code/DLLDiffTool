using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
//Refrence for using the "" Algorithm
using NReco.Text;
using System.Diagnostics;
using System.Threading;
using System.Reflection;

namespace DLLDiffTool
{
    class Program
    {
        class FileCompare : System.Collections.Generic.IEqualityComparer<System.IO.FileInfo>
        {
            public bool Equals(System.IO.FileInfo f1, System.IO.FileInfo f2)
            {
                return (f1.Name == f2.Name);
            }
            public int GetHashCode(System.IO.FileInfo fi)
            {
                string s = fi.Name;
                return s.GetHashCode();
            }
        }
        static void Main(string[] args)
        {
            try
            {
                // Code which takes up the 1 ES folder path
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(@"Please paste the complete '1ES' folder path below , where your dll resides and hit enter [ hint: \\cdmbuilds\builds\~~~~~~\debug\amd64\Modules\AI\YOUR.dll ]");
                Console.ResetColor();
                string oneESFolderPath = Console.ReadLine();

                // Code which takes up the CDM Build folder path
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(@"Please paste the complete 'CDMBuild' folder path below , where your dll resides and hit enter");
                Console.ResetColor();
                string cdmBuildFolderPath = Console.ReadLine();

                // Write code which can show up the list of files there in each 
                System.IO.DirectoryInfo dir1 = new System.IO.DirectoryInfo(oneESFolderPath);
                System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(cdmBuildFolderPath);

                // Will fecth dlls from all folder and subfolders
                IEnumerable<System.IO.FileInfo> list1 = dir1.GetFiles("*.dll", System.IO.SearchOption.AllDirectories);

                IEnumerable<System.IO.FileInfo> list2 = dir2.GetFiles("*.dll", System.IO.SearchOption.AllDirectories);

                // List out dlls which are present not in CDMBuild folder
                bool flag2 = dllsCheck(list1, list2);

                Process p = new Process();

                bool matchMakingDone = false;
                foreach (var dll1 in list1)
                {

                    string dllPath = string.Concat(oneESFolderPath, @"\", dll1.Name);
                    Assembly assembly = Assembly.ReflectionOnlyLoadFrom(dllPath);
                    string dll_txt_Name = string.Concat(assembly.ManifestModule.Name, ".txt");

                    // Getting the dll file size in KB
                    string length = GetFileSizeInKB(dllPath);

                    // CReatig the Seperate folders for ILCODE 
                    //Note: This Code may need auto cleanup in future
                    string ILCodePath = string.Concat(Environment.CurrentDirectory, @"\ILCodeTextFiles");
                    Directory.CreateDirectory(ILCodePath);

                    string temp_txt_Location = ILCodePath + @"\" + dll_txt_Name;
                    string strCmdText = @"ildasm /tok /byt " + dllPath + @" /out=" + temp_txt_Location;

                    matchMakingDone = true;
                    foreach (var dll2 in list2)
                    {
                        if (dll1.Name == dll2.Name)
                        {
                            Console.WriteLine("Comparing {0} & {1}", dll1.Name, dll2.Name);

                            

                            //Creating process which can Convert DLL to ILCODE(in Text format)
                            DllToILCode(p,strCmdText);

                            var entityMissDic1 = new Dictionary<string, int>() {
                                                                          {".class", 0},
                                                                          {".method", 0},
                                                                          {"interface", 0},
                                                                          {".property", 0},
                                                                          {".assembly", 0},
                                                                        };
                            // This part of the code reads the IL Code and Gets all the entities and shows up the count
                            bool flag1 = EntitiesReader(temp_txt_Location, length, dllPath, entityMissDic1);

                            // Shows the directory where we are going to push the IL CODE    
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Looking for Your IL Code ? It will be pushed to this location: " + "'{0}'", ILCodePath);
                            Console.ResetColor();
                            matchMakingDone = true;
                            break;
                        }
                        else
                        {
                            matchMakingDone = false;
                        }
                    }
                    if (!matchMakingDone)
                    {

                    }
                }
                p.Close();
                //Wait for user input
                Console.WriteLine("END");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("finally block.");
            }
        }

        private static bool dllsCheck(IEnumerable<System.IO.FileInfo> list1, IEnumerable<System.IO.FileInfo> list2)
        {

            //Compare files from 2 different locations & Diplay if it is not present in the 2nd folder(CDM BUild)
            bool IsInDestination = false;

            Console.WriteLine("List of files present/not present 1ES folder & CDMBuild Folder");
            Console.WriteLine("==============================================================");
            int noCount = 1;
            foreach (System.IO.FileInfo s in list1)
            {
                IsInDestination = true;
                foreach (System.IO.FileInfo s2 in list2)
                {
                    if (s.Name == s2.Name)
                    {
                        IsInDestination = true;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("{0} - {1}", noCount, s.Name);
                        Console.ResetColor();
                        break;
                    }
                    else
                    {
                        IsInDestination = false;
                    }
                }

                if (!IsInDestination)
                {

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("{0} - {1} - Not Present", noCount, s.Name);
                    Console.ResetColor();
                }
                noCount++;
            }
            return true;
        }

        private static void DllToILCode(Process p,string strCmdText)
        {
            
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.Start();
            p.StandardInput.WriteLine(@"cd E:\Shared_Tools");
            p.StandardInput.WriteLine("E:");
            p.StandardInput.WriteLine(strCmdText);
           
        }

        private static string GetFileSizeInKB(string dllPath)
        {
            FileInfo fi = new FileInfo(dllPath);
            string fileLength = fi.Length.ToString();
            string length = string.Empty;
            if (fi.Length >= (1 << 10))
                length = string.Format("{0}Kb", fi.Length >> 10);
            return length;
        }
        private static bool EntitiesReader(string temp_txt_Location, string length, string dllPath, Dictionary<string, int> entityMissDic1)
        {
            StreamReader sr = new StreamReader(temp_txt_Location);
            var line = sr.ReadToEnd();
            int currentCount = 0;
            var keywords = new Dictionary<string, int>() {
                  {".class", 0},
                  {".method", 0},
                  {"interface", 0},
                  {".property", 0},
                  {".assembly", 0},
                };
            var matcher = new AhoCorasickDoubleArrayTrie<int>(keywords);
            var text = line;
            matcher.ParseText(text, (hit) =>
            {
                switch (text.Substring(hit.Begin, hit.Length))
                {
                    case ".class":
                        {
                            keywords.TryGetValue(".class", out currentCount);
                            keywords[".class"] = currentCount + 1;
                            break;
                        }
                    case ".method":
                        {
                            keywords.TryGetValue(".method", out currentCount);
                            keywords[".method"] = currentCount + 1;
                            break;
                        }
                    case "interface":
                        {
                            keywords.TryGetValue("interface", out currentCount);
                            keywords["interface"] = currentCount + 1;
                            break;
                        }
                    case ".property":
                        {
                            keywords.TryGetValue(".property", out currentCount);
                            keywords[".property"] = currentCount + 1;
                            break;
                        }
                    case ".assembly":
                        {
                            keywords.TryGetValue(".assembly", out currentCount);
                            keywords[".assembly"] = currentCount + 1;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            });
            // DLL size
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Size of the dll which you are comparing is :" + "'{0}'", length);
            Console.ResetColor();
            Console.WriteLine();
            // Displays the count
            Console.WriteLine("Total number of Classes, Interfaces, Methods, Properties found for the DLL at this path  :" + "'{0}'", dllPath);
            Console.WriteLine("Classes = {0}", keywords[".class"]);
            Console.WriteLine("Methods = {0}", keywords[".method"]);
            Console.WriteLine("Interfaces = {0}", keywords["interface"]);
            Console.WriteLine("Properties = {0}", keywords[".property"]);
            Console.WriteLine("assembly = {0}", keywords[".assembly"]);
            Console.WriteLine();

            entityMissDic1[".class"] = keywords[".class"];
            entityMissDic1[".method"] = keywords[".method"];
            entityMissDic1["interface"] = keywords["interface"];
            entityMissDic1[".property"] = keywords[".property"];
            entityMissDic1[".assembly"] = keywords[".assembly"];

            //close the file
            sr.Close();
            return true;
        }
    }
}
