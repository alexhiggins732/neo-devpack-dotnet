using Neo.Compiler.MSIL;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Neo.Compiler
{
    public class Program
    {
        //Console.WriteLine("helo ha:"+args[0]); //普通输出
        //Console.WriteLine("<WARN> 这是一个严重的问题。");//警告输出，黄字
        //Console.WriteLine("<WARN|aaaa.cs(1)> 这是ee一个严重的问题。");//警告输出，带文件名行号
        //Console.WriteLine("<ERR> 这是一个严重的问题。");//错误输出，红字
        //Console.WriteLine("<ERR|aaaa.cs> 这是ee一个严重的问题。");//错误输出，带文件名
        //Console.WriteLine("SUCC");//输出这个表示编译成功
        //控制台输出约定了特别的语法
        public static void Main(string[] args)
        {
            //set console
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var log = new DefLogger();
            log.Log("Neo.Compiler.MSIL console app v" + Assembly.GetEntryAssembly().GetName().Version);

            if (args.Length == 0)
            {
                log.Log("need one param for DLL filename.");
                log.Log("Example:neon abc.dll");
                return;
            }

            string filename = args[0];
            string onlyname = Path.GetFileNameWithoutExtension(filename);
            string filepdb = onlyname + ".pdb";
            var path = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    Directory.SetCurrentDirectory(path);
                }
                catch
                {
                    log.Log("Could not find path: " + path);
                    Environment.Exit(-1);
                    return;
                }
            }

            ILModule mod = new ILModule(log);
            Stream fs;
            Stream fspdb = null;

            //open file
            try
            {
                fs = File.OpenRead(filename);

                if (File.Exists(filepdb))
                {
                    fspdb = File.OpenRead(filepdb);
                }

            }
            catch (Exception err)
            {
                log.Log("Open File Error:" + err.ToString());
                return;
            }
            //load module
            try
            {
                mod.LoadModule(fs, fspdb);
            }
            catch (Exception err)
            {
                log.Log("LoadModule Error:" + err.ToString());
                return;
            }
            byte[] bytes;
            bool bSucc;
            string jsonstr = null;
            //convert and build
            NeoModule neomodule = null;
            try
            {
                var conv = new ModuleConverter(log);
                ConvOption option = new ConvOption();
                neomodule = conv.Convert(mod, option);
                bytes = neomodule.Build();
                log.Log("convert succ");

                try
                {
                    var outjson = vmtool.FuncExport.Export(neomodule, bytes);
                    StringBuilder sb = new StringBuilder();
                    outjson.ConvertToStringWithFormat(sb, 0);
                    jsonstr = sb.ToString();
                    log.Log("gen abi succ");
                }
                catch (Exception err)
                {
                    log.Log("gen abi Error:" + err.ToString());
                }

            }
            catch (Exception err)
            {
                log.Log("Convert Error:" + err.ToString());
                return;
            }
            //write bytes
            try
            {

                string bytesname = onlyname + ".avm";

                File.Delete(bytesname);
                File.WriteAllBytes(bytesname, bytes);
                log.Log("write:" + bytesname);
                bSucc = true;
            }
            catch (Exception err)
            {
                log.Log("Write Bytes Error:" + err.ToString());
                return;
            }
            //write abi
            try
            {

                string abiname = onlyname + ".abi.json";

                File.Delete(abiname);
                File.WriteAllText(abiname, jsonstr);
                log.Log("write:" + abiname);
                bSucc = true;
            }
            catch (Exception err)
            {
                log.Log("Write abi Error:" + err.ToString());
                return;
            }
            //write sourcemap
            try
            {
                string sourcemap = SourceMapTool.GenMapFile(onlyname,neomodule);
                string sourcemapfile = onlyname + ".map";
                File.Delete(sourcemapfile);
                File.WriteAllText(sourcemapfile, sourcemap);
                log.Log("write:" + sourcemapfile);
            }
            catch (Exception err)
            {
                log.Log("Write SourceMap Error:" + err.ToString());
                return;
            }
            //clean
            try
            {
                fs.Dispose();
                if (fspdb != null)
                    fspdb.Dispose();
            }
            catch (Exception err)
            {
                log.Log("Clean Error:" + err.ToString());
            }

            if (bSucc)
            {
                log.Log("SUCC");
            }
        }
    }
}