using CommunityToolkit.Maui.Storage;
using System.Dynamic;

namespace FCLiveToolApplication;

public partial class VideoDownloadPage : ContentPage
{
    public VideoDownloadPage()
    {
        InitializeComponent();
    }
    //public long ReceiveSize = 0;
    //public long AllFilesize = 0;
    //public double DownloadProcess;
    //public int ThreadNum = 1;
    //public List<ThreadInfo> threadinfos;
    public List<DownloadVideoFileList> DownloadFileLists = new List<DownloadVideoFileList>();
    public List<string> LocalM3U8FilesList = new List<string>();

    private async void SelectLocalM3U8FileBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult != 0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
{
    { DevicePlatform.iOS, new[] { "com.apple.mpegurl" , "application/vnd.apple.mpegurl" } },
    { DevicePlatform.macOS, new[] {  "application/vnd.apple.mpegurl" } },
    { DevicePlatform.Android, new[] { "audio/x-mpegurl"  } },
    { DevicePlatform.WinUI, new[] { ".m3u8"} }
});

        var filePicker = await FilePicker.PickMultipleAsync(new PickOptions()
        {
            PickerTitle = "ѡ��M3U8�ļ�",
            FileTypes = fileTypes
        });

        if (filePicker is not null && filePicker.Count() > 0)
        {
            LocalM3U8FilesList = filePicker.Select(p => p.FullPath).ToList();
            LocalM3U8Tb.Text = "�Ѿ�ѡ����" + LocalM3U8FilesList.Count + "���ļ�";
        }
        else
        {
            LocalM3U8Tb.Text = "��ȡ��ѡ��";
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }
    }

    private async void SelectLocalM3U8FolderBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult != 0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        var folderPicker = await FolderPicker.PickAsync(FileSystem.AppDataDirectory, CancellationToken.None);

        if (folderPicker.IsSuccessful)
        {
            List<string> mlist = new List<string>();
            LoadM3U8FileFromSystem(folderPicker.Folder.Path, ref mlist);
            LocalM3U8FilesList=mlist;

            if(LocalM3U8FilesList.Count<1)
            {
                await DisplayAlert("��ʾ��Ϣ", "��ǰѡ���Ŀ¼��û��M3U8�ļ���������ѡ��", "ȷ��");
            }
            LocalM3U8Tb.Text = "�Ѿ�ѡ����" + LocalM3U8FilesList.Count + "���ļ�";

        }
        else
        {
            LocalM3U8Tb.Text = "��ȡ��ѡ��";
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }


    }
    public void LoadM3U8FileFromSystem(string path,ref List<string> list)
    {
        foreach (string item in Directory.EnumerateFileSystemEntries(path).ToList())
        {
            if (File.GetAttributes(item).HasFlag(FileAttributes.Directory))
            {
                LoadM3U8FileFromSystem(item, ref list);
            }
            else
            {
                if (item.ToLower().EndsWith(".m3u8"))
                {
                    if (LocalM3U8FilesList.Where(p => p==item).Count()<1)
                    {
                        list.Add(item);
                    }

                }

            }

        }
    }
    private async void M3U8AnalysisBtn_Clicked(object sender, EventArgs e)
    {
        List<VideoAnalysisList> ResultList = new List<VideoAnalysisList>();
        List<string> readresult=new List<string>();

        if (M3U8SourceRBtn1.IsChecked)
        {
            if (string.IsNullOrWhiteSpace(M3U8SourceURLTb.Text))
            {
                await DisplayAlert("��ʾ��Ϣ", "������ֱ��ԴM3U8��ַ��", "ȷ��");
                return;
            }
            if (!M3U8SourceURLTb.Text.Contains("://"))
            {
                await DisplayAlert("��ʾ��Ϣ", "��������ݲ�����URL�淶��", "ȷ��");
                return;
            }

            //�����������ʶ�𣬲��Ҽ�������ʶ��Ĳ���
            List<string> M3U8DownloadURLsList = new List<string>();
            M3U8DownloadURLsList.Add(M3U8SourceURLTb.Text);

            readresult = await new VideoManager().DownloadAndReadM3U8FileForDownloadTS(ResultList, M3U8DownloadURLsList, 0);
            if (readresult.Count<1)
            {
                return;
            }

        }
        else if (M3U8SourceRBtn2.IsChecked)
        {
            if (LocalM3U8FilesList.Count < 1)
            {
                await DisplayAlert("��ʾ��Ϣ", "��ǰû��ѡ���κ��ļ�������ѡ���ļ����ļ��У�", "ȷ��");
                return;
            }

            readresult = await new VideoManager().DownloadAndReadM3U8FileForDownloadTS(ResultList,LocalM3U8FilesList , 1);
            if(readresult.Count<1)
            {
                return;
            }
            int needAddServerCount = readresult.Where(p => p.StartsWith("CODE_")).Count();
            if (needAddServerCount> 0)
            {
                bool tresult = await DisplayAlert("��ʾ��Ϣ", "��ǰ��"+needAddServerCount+"������ֱ��Դ�ļ��ڵķ�Ƭ�ļ�URL����Ե�ַ�������޷�֪�����ǵķ�������" +
                    "������Ҫ�㲹��ֱ��Դ��Ӧ�ķ�������ַ����Ҫ�������仹��������Щ�ļ���", "����", "����");
                if (tresult)
                {
                    for(int i = 0;i<readresult.Count;i++)
                    {
                        if (readresult[i].StartsWith("CODE_"))
                        {
                            string urlnewvalue = await DisplayPromptAsync("��ӷ�����", "������ֱ��Դ�ķ�������ַ���õ�ַ�����ļ�������������Ҫ������\n"+
                                "�����ļ��ĵ�ַ�� https://example.com/abc/123.ts ����ô��Ӧ����д https://example.com/abc/", "���沢��һ��", "ȡ��", "URL...", -1, Keyboard.Text, "");
                           
                            if (string.IsNullOrWhiteSpace(urlnewvalue))
                            {
                                if (urlnewvalue!=null)
                                {
                                    await DisplayAlert("��ʾ��Ϣ", "��������ȷ�����ݣ�", "ȷ��");
                                    i--;
                                    continue;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (!urlnewvalue.Contains("://"))
                            {
                                await DisplayAlert("��ʾ��Ϣ", "��������ݲ�����URL�淶��", "ȷ��");
                                i--;
                                continue;
                            }

                            if(!urlnewvalue.EndsWith("/"))
                            {
                                urlnewvalue+="/";
                            }

                            //���û������������֮ǰ�ĵ�ַ����ƴ��
                            ResultList[i].TS_PARM.ForEach(p=>
                            {
                                p.FullURL=urlnewvalue+p.FullURL;
                            });

                            //���ñ�ʶΪ��ֵ��Ĭ���û����������ȷ�ķ�����
                            readresult[i]="";
                        }

                    }

                }

                //���ʣ���CODE_��ʶ����Ϊû�б���������
                for (int i = readresult.Count-1; i>=0; i--)
                {
                    if (readresult[i].StartsWith("CODE_"))
                    {
                        readresult.RemoveAt(i);
                        ResultList.RemoveAt(i);
                    }
                }


                //Ҫ�ж�readresult�Ƿ�Ϊ��
                if(readresult.Count<1)
                {
                    await DisplayAlert("��ʾ��Ϣ", "���β�������µ���Ŀ����Ϊѡ����б���û����Ч��ֱ��Դ��", "ȷ��");
                    return;
                }


            }

        }


        var trList = readresult.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if(trList.Count > 0)
        {
            string tmsg = "";
            for (int i = readresult.Count-1;i>=0 ; i--)
            {
                if (!string.IsNullOrWhiteSpace(readresult[i]))
                {
                    tmsg=ResultList[i].FullURL+"\n"+ readresult[i]+"\n\n"+tmsg;
                    //�Ƴ�ȫ��ʧЧ
                    readresult.RemoveAt(i);
                    ResultList.RemoveAt(i);
                }

            }
            await DisplayAlert("��ʾ��Ϣ", "��ǰ��һ������ֱ��Դ�������⣬��ϸ�������£�\n\n\n"+tmsg, "ȷ��");

            if(ResultList.Count<1)
            {
                await DisplayAlert("��ʾ��Ϣ", "���β�������µ���Ŀ����Ϊѡ����б���û����Ч��ֱ��Դ��", "ȷ��");
                return;
            }
        }

        

        List<VideoAnalysisList> videoAnalysisLists = new List<VideoAnalysisList>();
        if (VideoAnalysisList.ItemsSource != null)
        {
            videoAnalysisLists = VideoAnalysisList.ItemsSource.Cast<VideoAnalysisList>().ToList();

            videoAnalysisLists.ForEach(p=>
            {
                var titem = ResultList.FirstOrDefault(p2 => p2.FullURL == p.FullURL);
                if (titem != null)
                {
                    videoAnalysisLists.Remove(p);
                }
            });

        }
        videoAnalysisLists.AddRange(ResultList);

        VideoAnalysisList.ItemsSource = videoAnalysisLists;
    }

    private void M3U8SourceRBtn_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        RadioButton entry = sender as RadioButton;

        if (entry.StyleId == "M3U8SourceRBtn1")
        {
            M3U8SourceURLTb.IsVisible = true;
            LocalM3U8SelectPanel.IsVisible = false;
        }
        else if (entry.StyleId == "M3U8SourceRBtn2")
        {
            M3U8SourceURLTb.IsVisible = false;
            LocalM3U8SelectPanel.IsVisible = true;
        }

    }

    private async void M3U8DownloadBtn_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SaveDownloadFolderTb.Text))
        {
            await DisplayAlert("��ʾ��Ϣ", "����ѡ�������ļ�Ҫ�����λ�ã�", "ȷ��");
            return;
        }
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult != 0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫд���ļ���", "ȷ��");
            return;
        }
        if (!Directory.Exists(SaveDownloadFolderTb.Text))
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�����ļ�����λ�õ�Ŀ¼�����ڣ�������ѡ��", "ȷ��");
            return;
        }
        if (VideoAnalysisList.ItemsSource is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�Ϊ�գ����Ȼ�ȡһ��M3U8ֱ��Դ��", "ȷ��");
            return;
        }

        var tlist = VideoAnalysisList.ItemsSource.Cast<VideoAnalysisList>().Where(p => p.IsSelected).ToList();
        for (int i = 0; i < tlist.Count; i++)
        {
            var tobj = tlist[i];
            VideoManager vmanager = new VideoManager();
            if (DownloadFileLists.Where(p => p.M3U8FullLink == tobj.FullURL && p.CurrentActiveObject.isContinueDownloadStream).Count() > 0)
            {
                await DisplayAlert("��ʾ��Ϣ", "�㵱ǰ�����������M3U8ֱ������" + tobj.FileName + " �������ظ��������", "ȷ��");
                continue;
            }
            DownloadFileLists.Add(new DownloadVideoFileList() { SaveFilePath = SaveDownloadFolderTb.Text, CurrentVALIfm = tobj, CurrentActiveObject = vmanager });

            new Thread(async () =>
            {
                string r="";
#if ANDROID
                r = await vmanager.DownloadM3U8Stream(tobj, SaveDownloadFolderTb.Text + "/", true);
#else
r = await vmanager.DownloadM3U8Stream(tobj, SaveDownloadFolderTb.Text + "\\", true);
#endif

                if (r != "")
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DisplayAlert("��ʾ��Ϣ", tobj.FileName + "\n" + r, "ȷ��");
                    });

                }
            }).Start();
        }

        DownloadVideoFileList.ItemsSource = DownloadFileLists;

    }

    private void VideoAnalysisListCB_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (VideoAnalysisList.ItemsSource is null)
        {
            return;
        }

        var tlist = VideoAnalysisList.ItemsSource.Cast<VideoAnalysisList>().ToList();
        tlist.ForEach(p => { p.IsSelected = e.Value; });
        VideoAnalysisList.ItemsSource = tlist;
    }

    /*
         public async Task<string> DownloadM3U8Stream(List<M3U8_TS_PARM> mlist,string savepath,string filename)
        {
            threadinfos= new List<ThreadInfo>();
            int FileIndex = 0;

            foreach (var m in  mlist)
            {
                string url = m.FullURL;
                using (HttpClient httpClient = new HttpClient())
                {
                    int statusCode;
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
                    HttpResponseMessage response = null;

                    try
                    {
                        response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                        statusCode=(int)response.StatusCode;
                        if (!response.IsSuccessStatusCode)
                        {
                            return "����ʧ�ܣ�"+"HTTP������룺"+statusCode;
                        }

                        AllFilesize = response.Content.Headers.ContentLength??-1;
                        if (AllFilesize<=0)
                        {
                            return "�޷��� ContentLength �л�ȡ��Ч���ļ���С��";
                        }

                    }
                    catch (Exception)
                    {
                        return "�������쳣��";
                    }


                    List<Task> taskList = new List<Task>();
                    int FinishTaskCount = 0;
                    ReceiveSize=0;

                    //�˴�Ҫ����������ȡM3U8����

                    long pieceSize = (long)AllFilesize / ThreadNum + (long)AllFilesize % ThreadNum;
                    for (int i = 0; i < ThreadNum; i++)
                    {
                        ThreadInfo currentThread = new ThreadInfo();
                        currentThread.ThreadId = i;
                        currentThread.ThreadStatus = false;

                        currentThread.TmpFileName = string.Format($"{savepath}TMP{FileIndex}_{filename}.tmp");
                        currentThread.Url = url;
                        currentThread.FileName = filename+".mp4";

                        long startPosition = (i * pieceSize);
                        currentThread.StartPosition = startPosition == 0 ? 0 : startPosition + 1;
                        currentThread.FileSize = startPosition + pieceSize;

                        threadinfos.Add(currentThread);

                        taskList.Add(Task.Factory.StartNew(async () =>
                        {
                            string r = await ReceiveHttp(currentThread);
                            if (r!="")
                            {
                                await DisplayAlert("��ʾ��Ϣ", filename+"\n"+ r, "ȷ��");
                            }

                            FinishTaskCount++;
                        }));
                        FileIndex++;
                    }


                    while (true)
                    {
                        if (FinishTaskCount==taskList.Count)
                        {
                            break;
                        }
                    }    

                }
            }




            MergeFile(savepath+filename+".mp4");
            threadinfos.Clear();

            return "";
        }
        public async Task<string> ReceiveHttp(object thread)
        {
            FileStream fs = null;
            Stream ns = null;
            try
            {
                ThreadInfo currentThread = (ThreadInfo)thread;

                //���������ļ��Ѵ��ڵ��ж�
                if (!File.Exists(currentThread.FileName))
                {
                    fs = new FileStream(currentThread.TmpFileName, FileMode.Create);


                    using (HttpClient httpClient = new HttpClient())
                    {
                        int statusCode;
                        httpClient.DefaultRequestHeaders.Add("Accept-Ranges", "bytes");
                        httpClient.DefaultRequestHeaders.Add("Range", "bytes="+currentThread.StartPosition+"-"+(currentThread.FileSize));
                        HttpResponseMessage response = null;

                        try
                        {
                            response = await httpClient.GetAsync(currentThread.Url);

                            statusCode=(int)response.StatusCode;
                            if (!response.IsSuccessStatusCode)
                            {
                                return "����ʧ�ܣ�"+"HTTP������룺"+statusCode;
                            }

                        }
                        catch (Exception)
                        {
                            return "�������쳣��";
                        }


                       ns = await response.Content.ReadAsStreamAsync();
                       ns.CopyTo(fs);
                    }

                    ReceiveSize += ns.Length;
                    double percent = (double)ReceiveSize / (double)AllFilesize * 100;

                    DownloadProcess=percent;


                }
                currentThread.ThreadStatus = true;
            }
            catch 
            {
                return "����ʱ�����쳣";
            }
            finally
            {
                fs?.Close();
                ns?.Close();
            }

            return "";
        }

        public class ThreadInfo
        {
            public int ThreadId { get; set; }
            public bool ThreadStatus { get; set; }
            public long StartPosition { get; set; }
            public long FileSize { get; set; }
            public string Url { get; set; }
            public string TmpFileName { get; set; }
            public string FileName { get; set; }
            public int Times { get; set; }
        }

        private void MergeFile(string filepath)
    {
        string downFileNamePath = filepath;
        int length = 0;
        using (FileStream fs = new FileStream(downFileNamePath, FileMode.Create))
        {
            foreach (var item in threadinfos.OrderBy(o => o.ThreadId))
            {
                if (!File.Exists(item.TmpFileName)) continue;
                var tempFile = item.TmpFileName;

                try
                {
                    using (FileStream tempStream = new FileStream(tempFile, FileMode.Open))
                    {
                        byte[] buffer = new byte[tempStream.Length];

                        while ((length = tempStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, length);
                        }
                        tempStream.Flush();
                    }

                }
                catch
                {

                }

                try
                {
                    File.Delete(item.TmpFileName);
                }
                catch (Exception)
                {

                }

            }
        }

    }
     */

    private async void SelectSaveDownloadFolderBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult != 0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        var folderPicker = await FolderPicker.PickAsync(FileSystem.AppDataDirectory, CancellationToken.None);

        if (folderPicker.IsSuccessful)
        {
            SaveDownloadFolderTb.Text = folderPicker.Folder.Path;
        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }
    }

    private void DownloadFileListCB_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (DownloadVideoFileList.ItemsSource is null)
        {
            return;
        }

        var tlist = DownloadVideoFileList.ItemsSource.Cast<DownloadVideoFileList>().ToList();
        tlist.ForEach(p => { p.IsSelected = e.Value; });
        DownloadVideoFileList.ItemsSource = tlist;
    }

    private async void DownloadFileStopBtn_Clicked(object sender, EventArgs e)
    {
        if (DownloadVideoFileList.ItemsSource is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�Ϊ�գ�", "ȷ��");
            return;
        }

        var tlist = DownloadVideoFileList.ItemsSource.Cast<DownloadVideoFileList>().Where(p => p.IsSelected).ToList();
        if (tlist.Count < 1)
        {
            await DisplayAlert("��ʾ��Ϣ", "�������ٹ�ѡһ��Ҫֹͣ������", "ȷ��");
            return;
        }

        tlist.ForEach(p =>
        {
            var tl = DownloadFileLists.Where(p2 => p2 == p).FirstOrDefault();
            tl.CurrentActiveObject.isContinueDownloadStream = false;
            //DownloadFileLists.Remove(tl);
        });
        DownloadFileLists.RemoveAll(p => !p.CurrentActiveObject.isContinueDownloadStream || p.CurrentActiveObject.isEndList);


        DownloadVideoFileList.ItemsSource = DownloadFileLists;
        await DisplayAlert("��ʾ��Ϣ", "��ֹͣѡ��������", "ȷ��");

    }
}