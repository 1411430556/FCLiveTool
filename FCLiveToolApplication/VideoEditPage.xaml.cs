using CommunityToolkit.Maui.Storage;
using Microsoft.Maui;
using System.Text;

namespace FCLiveToolApplication;

public partial class VideoEditPage : ContentPage
{
    public VideoEditPage()
    {
        InitializeComponent();
    }
    public static VideoEditPage videoEditPage;
    public List<LocalM3UList> CurrentLocalM3UList = new List<LocalM3UList>();
    public string CurrentSaveLocation = "";
    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (videoEditPage!=null)
        {
            return;
        }

        videoEditPage=this;
        VideoEditList.ItemTemplate =new VideoEditListDataTemplateSelector();
    }

    private async void SelectLocalM3UFolderBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        //ÿ�ε����ť����ǰ�б����ݷ������������ʱ���жԱ�
        if (LocalM3UList.ItemsSource is not null)
        {
            CurrentLocalM3UList=LocalM3UList.ItemsSource.Cast<LocalM3UList>().ToList();
        }
        //VideoWindow.Source=new Uri("C:\\Users\\Lee\\Desktop\\cgtn-f.m3u8");
        var folderPicker = await FolderPicker.PickAsync(FileSystem.AppDataDirectory, CancellationToken.None);

        if (folderPicker.IsSuccessful)
        {
            List<LocalM3UList> mlist = new List<LocalM3UList>();
            LoadM3UFileFromSystem(folderPicker.Folder.Path, ref mlist);
            CurrentLocalM3UList.AddRange(mlist);

            //����������
            int tmindex = 0;
            CurrentLocalM3UList.ForEach(p =>
            {
                p.ItemId="LMRB"+tmindex;
                tmindex++;
            });
            LocalM3UList.ItemsSource=CurrentLocalM3UList;
        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }
    }

    private async void SelectLocalM3UFileBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        //ÿ�ε����ť����ǰ�б����ݷ������������ʱ���жԱ�
        if (LocalM3UList.ItemsSource is not null)
        {
            CurrentLocalM3UList=LocalM3UList.ItemsSource.Cast<LocalM3UList>().ToList();
        }

        var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
{
    { DevicePlatform.iOS, new[] { "com.apple.mpegurl" , "application/vnd.apple.mpegurl" } },
    { DevicePlatform.macOS, new[] {  "application/vnd.apple.mpegurl" } },
    { DevicePlatform.Android, new[] { "audio/x-mpegurl"  } },
    { DevicePlatform.WinUI, new[] { ".m3u"} }
});

        var filePicker = await FilePicker.PickMultipleAsync(new PickOptions()
        {
            PickerTitle="ѡ��M3U�ļ�",
            FileTypes=fileTypes
        });

        if (filePicker is not null&&filePicker.Count()>0)
        {
            filePicker.ToList().ForEach(p =>
            {
                if (CurrentLocalM3UList.Where(p2 => p2.FullFilePath==p.FullPath).Count()<1&&p.FileName.ToLower().EndsWith(".m3u"))
                {
                    CurrentLocalM3UList.Add(new LocalM3UList() { FileName=p.FileName, FilePath=p.FullPath.Replace(p.FileName, ""), FullFilePath=p.FullPath });
                }
            });

            //����������
            int tmindex = 0;
            CurrentLocalM3UList.ForEach(p =>
            {
                p.ItemId="LMRB"+tmindex;
                tmindex++;
            });
            LocalM3UList.ItemsSource=CurrentLocalM3UList;
        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }

    }

    private void ClearLocalM3UBtn_Clicked(object sender, EventArgs e)
    {
        LocalM3UList.ItemsSource=null;
        CurrentLocalM3UList.Clear();
    }

    public void LoadM3UFileFromSystem(string path, ref List<LocalM3UList> list)
    {
        foreach (string item in Directory.EnumerateFileSystemEntries(path).ToList())
        {
            if (File.GetAttributes(item).HasFlag(FileAttributes.Directory))
            {
                LoadM3UFileFromSystem(item, ref list);
            }
            else
            {
                if (item.ToLower().EndsWith(".m3u"))
                {
                    string tname;
#if ANDROID
                    tname = item.Substring(item.LastIndexOf("/")+1);
#else
                    tname = item.Substring(item.LastIndexOf("\\")+1);
#endif
                    //string tfoldername = "."+item.Replace(initFoldername, "").Replace(tname, "");
                    string tfoldername = item.Replace(tname, "");

                    if (CurrentLocalM3UList.Where(p => p.FullFilePath==item).Count()<1)
                    {
                        list.Add(new LocalM3UList() { FileName=tname, FilePath=tfoldername, FullFilePath=item });
                    }

                }

            }

        }
    }
    private async void LocalM3UList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        CurrentSaveLocation = "";
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        LocalM3UList m3ulist = e.Item as LocalM3UList;
        if (!File.Exists(m3ulist.FullFilePath))
        {
            await DisplayAlert("��ʾ��Ϣ", "�����Ҳ�����ֱ��Դ�����ļ��������Ǹ��ļ��ѱ�ɾ�����ƶ���", "ȷ��");
            return;
        }

        try
        {
            CurrentSaveLocation = m3ulist.FullFilePath;
            VideoEditList.ItemsSource=await new VideoManager().ReadM3UString(File.ReadAllText(m3ulist.FullFilePath));
        }
        catch (Exception)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ȡM3U�ļ�����ʱ�����쳣", "ȷ��");
        }

#if ANDROID
        VideoEditPanelBtn_Clicked(sender, e);
#endif
    }
    private async void LocalM3URemoveBtn_Clicked(object sender, EventArgs e)
    {
        Button LMRBtn = sender as Button;
        List<LocalM3UList> tlist = LocalM3UList.ItemsSource.Cast<LocalM3UList>().ToList();

        var item = tlist.Where(p => p.ItemId==LMRBtn.CommandParameter.ToString()).FirstOrDefault();
        if (item is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�Ƴ�ʱ�����쳣", "ȷ��");
            return;
        }
        tlist.Remove(item);

        LocalM3UList.ItemsSource=tlist;
    }
    private void LocalM3UList_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ItemsSource")
        {
            if (LocalM3UList.ItemsSource is not null&&LocalM3UList.ItemsSource.Cast<LocalM3UList>().Count()>0)
            {
                LocalM3UIfmText.Text="";
            }
            else
            {
                LocalM3UIfmText.Text="��ǰ�б���û��M3Uֱ��Դ�ļ�����ȥ��Ӱ�~";
            }
        }
    }

    private void VideoEditList_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ItemsSource")
        {
            if (VideoEditList.ItemsSource is not null&&VideoEditList.ItemsSource.Cast<VideoEditList>().Count()>0)
            {
                VideoEditIfmText.Text="";
            }
            else
            {
                VideoEditIfmText.Text="��ǰ�����б�Ϊ�գ���������б�ѡһ��M3Uֱ��Դ";
            }
        }
    }

    private void VideoEditListEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        Entry entry = sender as Entry;
        if (entry != null)
        {
            if (entry.BindingContext is VideoEditListTVG tcontext)
            {
                string oldvalue = e.OldTextValue;
                string newvalue = e.NewTextValue;
                bool isNewStr = false;

                if (string.IsNullOrWhiteSpace(oldvalue)&&string.IsNullOrWhiteSpace(newvalue))
                {
                    return;
                }

                switch (entry.StyleId)
                {
                    case "VELTVG1":
                        if(!tcontext.AllStr.Contains("group-title="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="group-title=\""+oldvalue+"\"";
                        newvalue="group-title=\""+newvalue+"\"";
                        break;
                    case "VELTVG2":
                        if (!tcontext.AllStr.Contains("tvg-group="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-group=\""+oldvalue+"\"";
                        newvalue="tvg-group=\""+newvalue+"\"";
                        break;
                    case "VELTVG3":
                        if (!tcontext.AllStr.Contains("tvg-id="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-id=\""+oldvalue+"\"";
                        newvalue="tvg-id=\""+newvalue+"\"";
                        break;
                    case "VELTVG4":
                        if (!tcontext.AllStr.Contains("tvg-logo="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-logo=\""+oldvalue+"\"";
                        newvalue="tvg-logo=\""+newvalue+"\"";
                        break;
                    case "VELTVG5":
                        if (!tcontext.AllStr.Contains("tvg-country="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-country=\""+oldvalue+"\"";
                        newvalue="tvg-country=\""+newvalue+"\"";
                        break;
                    case "VELTVG6":
                        if (!tcontext.AllStr.Contains("tvg-language="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-language=\""+oldvalue+"\"";
                        newvalue="tvg-language=\""+newvalue+"\"";
                        break;
                    case "VELTVG7":
                        if (!tcontext.AllStr.Contains("tvg-name="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-name=\""+oldvalue+"\"";
                        newvalue="tvg-name=\""+newvalue+"\"";
                        break;
                    case "VELTVG8":
                        if (!tcontext.AllStr.Contains("tvg-url="))
                        {
                            isNewStr=true;
                        }

                        oldvalue="tvg-url=\""+oldvalue+"\"";
                        newvalue="tvg-url=\""+newvalue+"\"";
                        break;
                }

                if(isNewStr)
                {
                    if(string.IsNullOrWhiteSpace(tcontext.AllStr))
                    {
                        tcontext.AllStr="#EXTINF: "+newvalue+",";
                    }
                    else
                    {
                        if (!tcontext.AllStr.StartsWith("#EXTINF:"))
                        {
                            tcontext.AllStr="#EXTINF: "+tcontext.AllStr;
                        }

                        var tindex = tcontext.AllStr.LastIndexOf(",");
                        if (tindex<0)
                        {
                            tcontext.AllStr+=" "+newvalue+",";
                        }
                        else
                        {
                            tcontext.AllStr=tcontext.AllStr.Insert(tindex, " "+newvalue);
                        }
                    }

                }
                else
                {
                    if (!tcontext.AllStr.StartsWith("#EXTINF:"))
                    {
                        tcontext.AllStr="#EXTINF: "+tcontext.AllStr;
                    }

                    tcontext.AllStr=tcontext.AllStr.Replace(oldvalue, newvalue);
                }

            }

            /*
                         var videoEditListTVG = VideoEditList.ItemsSource.Cast<VideoEditList>().Where(p => p.ItemName==test.ItemName).ToList().FirstOrDefault();
                        VideoEditListTVG test2 = videoEditListTVG as VideoEditListTVG;
                        test2.AllStr="��������"+entry.Text;
             */
        }
    }

    private async void VideoEditListAddItemBtn_Clicked(object sender, EventArgs e)
    {
        List<VideoEditList> videoEditList=new List<VideoEditList>();
        if (VideoEditList.ItemsSource != null)
        {
            videoEditList = VideoEditList.ItemsSource.Cast<VideoEditList>().ToList();
        }

        string MSelectResult = await DisplayActionSheet("��Ҫ���ʲô���ݣ�", "ȡ��", null, new string[] { "TVG��ǩ", "EXT��ǩ", "ֱ��Դ�ĵ�ַ", "M3U�ļ���ͷ", "�����ַ���" });
        if (MSelectResult == "ȡ��"||MSelectResult is null)
        {
            return;
        }
        if (MSelectResult =="M3U�ļ���ͷ")
        {
            if (videoEditList.Where(p => p.ItemTypeId==3).Count()>0)
            {
                await DisplayAlert("��ʾ��Ϣ", "��ǰ�б��Ѿ���M3U�ļ���ͷ����Ҫ����µ������Ƴ�֮ǰ��M3U�ļ���ͷ��", "ȷ��");
                return;
            }

            videoEditList.Insert(0, new VideoEditListEXT_Readonly() { ItemTypeId=3, EXTTag="#EXTM3U" });
            VideoEditList.ItemsSource=videoEditList;
            return;
        }

        string MSelectResult2 = await DisplayActionSheet("��Ҫ���б��ĸ��ط��������У�", "ȡ��", null, new string[] { "��ͷ", "��β", "�б�ѡ�е���һ�еĺ���", "ÿһ���ѹ�ѡ�ĸ�ѡ���Ӧ����һ�еĺ���" });
        if (MSelectResult2 == "ȡ��"||MSelectResult2 is null)
        {
            return;
        }


        switch (MSelectResult2)
        {
            case "��ͷ":
                if(videoEditList.Count<1)
                {
                    await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�û��M3U�ļ���ͷ���������һ��M3U�ļ���ͷ��", "ȷ��");
                    return;
                }
                if(videoEditList.Where(p=>p.ItemTypeId==3).Count()<1)
                {
                    videoEditList.Insert(0, GetTypeIDFromVEL(MSelectResult));
                }
                else
                {
                    videoEditList.Insert(1, GetTypeIDFromVEL(MSelectResult));
                }

                break;
            case "��β":
                if (videoEditList.Count<1)
                {
                    await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�û��M3U�ļ���ͷ���������һ��M3U�ļ���ͷ��", "ȷ��");
                    return;
                }

                videoEditList.Insert(videoEditList.Count, GetTypeIDFromVEL(MSelectResult));
                break;
            case "�б�ѡ�е���һ�еĺ���":

                if (VideoEditList.SelectedItem is null)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�㵱ǰû��ѡ���κ�������б�ѡ��һ����ǹ�ѡ��ѡ��", "ȷ��");
                    return;
                }
                if (videoEditList.Count<1)
                {
                    await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�û��M3U�ļ���ͷ���������һ��M3U�ļ���ͷ��", "ȷ��");
                    return;
                }

                videoEditList.Insert(videoEditList.IndexOf(VideoEditList.SelectedItem as VideoEditList)+1, GetTypeIDFromVEL(MSelectResult));

                break;
            case "ÿһ���ѹ�ѡ�ĸ�ѡ���Ӧ����һ�еĺ���":

                var tlist = videoEditList.Where(p => p.IsSelected==true).ToList();
                if (tlist.Count<1)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�㵱ǰû�й�ѡ�κ�������ٹ�ѡһ����ѡ��", "ȷ��");
                    return;
                }
                for (int i = tlist.Count-1; i>=0; i--)
                {
                    videoEditList.Insert(videoEditList.IndexOf(tlist[i])+1, GetTypeIDFromVEL(MSelectResult));
                }

                /*
                                 tlist.OrderByDescending(p=>p).ToList().ForEach(p2 =>
                                {
                                    videoEditList.Insert(videoEditList.IndexOf(p2)+1, GetTypeIDFromVEL(MSelectResult));
                                });
                 */
                break;
        }

        VideoEditList.ItemsSource=videoEditList;
    }
    public VideoEditList GetTypeIDFromVEL(string keyword)
    {
        switch (keyword)
        {
            case "EXT��ǩ":
                return new VideoEditListEXT() { ItemTypeId=1, EXTTag="" };
            case "TVG��ǩ":
                return new VideoEditListTVG() { ItemTypeId=2, AllStr="", GroupTitle="", TVGGroup="", TVGID="", TVGLogo="", TVGCountry="", TVGLanguage="", TVGName="", TVGURL="" };
            case "ֱ��Դ�ĵ�ַ":
                return new VideoEditListSourceLink() { ItemTypeId=4, SourceLink="" };
            case "�����ַ���":
                return new VideoEditListOtherString() { ItemTypeId=5, AllStr="" };
        }
        return null;
    }
    private async void VideoEditListRemoveItemBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoEditList.ItemsSource is null)
        {
            return;
        }
        var videoEditList = VideoEditList.ItemsSource.Cast<VideoEditList>().ToList();
        if (videoEditList.Count <1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�û���κ��", "ȷ��");
            return;
        }

        string MSelectResult = await DisplayActionSheet("ȷ��Ҫɾ��ָ��������", "ȡ��", null, new string[] { "ɾ���б�ѡ�е���һ��", "ɾ��ÿһ���ѹ�ѡ�ĸ�ѡ���Ӧ����һ��" });
        if (MSelectResult == "ȡ��"||MSelectResult is null)
        {
            return;
        }
        if (MSelectResult.Contains("ɾ���б�"))
        {
            if (VideoEditList.SelectedItem is null)
            {
                await DisplayAlert("��ʾ��Ϣ", "�㵱ǰû��ѡ���κ�������б�ѡ��һ����ǹ�ѡ��ѡ��", "ȷ��");
                return;
            }
            var tlist= VideoEditList.SelectedItem as VideoEditList;
            if (tlist.ItemTypeId==3)
            {
                bool tresult = await DisplayAlert("��ʾ��Ϣ", "��⵽�㹴ѡ��M3U�ļ���ͷ���Ƿ�ȷ��Ҫ�Ƴ���", "��", "��");
                if (!tresult)
                {
                    return;
                }
            }
            videoEditList.Remove(tlist);
        }
        else if (MSelectResult.Contains("ɾ��ÿһ���ѹ�ѡ"))
        {
            var tlist = videoEditList.Where(p => p.IsSelected==true).ToList();
            if (tlist.Count<1)
            {
                await DisplayAlert("��ʾ��Ϣ", "�㵱ǰû�й�ѡ�κ�������ٹ�ѡһ����ѡ��", "ȷ��");
                return;
            }

            for(int i = 0; i < tlist.Count; i++)
            {
                if (tlist[i].ItemTypeId==3)
                {
                    bool tresult = await DisplayAlert("��ʾ��Ϣ", "��⵽�㹴ѡ��M3U�ļ���ͷ���Ƿ�ȷ��Ҫ�Ƴ���", "��", "��");
                    if (!tresult)
                    {
                        continue;
                    }
                }
                videoEditList.Remove(tlist[i]);
            }
        }

        VideoEditList.ItemsSource=videoEditList;
    }

    private async void VideoEditListSaveBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoEditList.ItemsSource is null)
        {
            return;
        }
        var videoEditList = VideoEditList.ItemsSource.Cast<VideoEditList>().ToList();
        if (videoEditList.Count <1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�û���κ��", "ȷ��");
            return;
        }
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ�����ļ���", "ȷ��");
            return;
        }


        string[] toptions;
        string tmessage;

        //if(CurrentSaveLocation!=""&&DeviceInfo.Platform!=DevicePlatform.Android)
        if(CurrentSaveLocation!="")
        {
            tmessage="��Ҫ��α��棿";
            toptions=new string[] { "���浽Դ�ļ�","���Ϊ..."};
        }
        else
        {
            tmessage="�����ť����";
            toptions=new string[] { "���Ϊ..." };
        }

        string MSelectResult = await DisplayActionSheet(tmessage, "ȡ��", null,toptions);
        if (MSelectResult == "ȡ��"||MSelectResult is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
            return;
        }
        if(MSelectResult =="���浽Դ�ļ�")
        {
            if (!File.Exists(CurrentSaveLocation))
            {
                await DisplayAlert("��ʾ��Ϣ", "�����Ҳ�����ֱ��Դ�����ļ��������Ǹ��ļ��ѱ�ɾ�����ƶ���", "ȷ��");
                CurrentSaveLocation="";
                return;
            }

            File.WriteAllText(CurrentSaveLocation, GetVELSaveStr(videoEditList));
            await DisplayAlert("��ʾ��Ϣ", "�ļ��ѳɹ���������\n"+CurrentSaveLocation, "ȷ��");
        }
        else
        {
            string tsavename;
            var tlist = LocalM3UList.SelectedItem;
            if (tlist!=null)
            {
                tsavename=(tlist as LocalM3UList).FileName;
            }
            else
            {
                tsavename=".m3u";
            }

            VideoEditSaveRing.IsRunning=true;
            new Thread(async()=>
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(GetVELSaveStr(videoEditList))))
                {
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async() =>
                        {
                            var fileSaver = await FileSaver.SaveAsync(FileSystem.AppDataDirectory, tsavename, ms, CancellationToken.None);

                            if (fileSaver.IsSuccessful)
                            {
                                await DisplayAlert("��ʾ��Ϣ", "�ļ��ѳɹ���������\n"+fileSaver.FilePath, "ȷ��");
                            }
                            else
                            {
                                //��ʱ�ж�Ϊ�û���ѡ��Ŀ¼ʱ�����ȡ����ť
                                await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
                            }

                            VideoEditSaveRing.IsRunning=false;
                        });


                    }
                    catch (Exception)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ�������û��Ȩ�ޡ�", "ȷ��");
                            VideoEditSaveRing.IsRunning=false;
                        });

                    }
                }
            }).Start();
        }


    }

    public string GetVELSaveStr(List<VideoEditList> videoEditList)
    {
        string saveStr = "";
        for(int i=0;i<videoEditList.Count; i++)
        {
            switch (videoEditList[i].ItemTypeId)
            {
                case 1:
                    saveStr+=(videoEditList[i] as VideoEditListEXT).EXTTag;
                    break;
                case 2:
                    saveStr+=(videoEditList[i] as VideoEditListTVG).AllStr;
                    break;
                case 3:
                    saveStr+=(videoEditList[i] as VideoEditListEXT_Readonly).EXTTag;
                    break;
                case 4:
                    saveStr+=(videoEditList[i] as VideoEditListSourceLink).SourceLink;
                    break;
                case 5:
                    saveStr+=(videoEditList[i] as VideoEditListOtherString).AllStr;
                    break;
            }

            if(i<videoEditList.Count-1)
            {
                saveStr+="\r\n";
            }
        }

        return saveStr;
    }

    private void LocalM3UPanelBtn_Clicked(object sender, EventArgs e)
    {
        LocalM3UPanel.IsVisible=true;
        LocalM3UPanel2.IsVisible=true;
        VideoEditPanel.IsVisible=false;
        VideoEditPanel2.IsVisible=false;
    }

    private void VideoEditPanelBtn_Clicked(object sender, EventArgs e)
    {
        LocalM3UPanel.IsVisible=false;
        LocalM3UPanel2.IsVisible=false;
        VideoEditPanel.IsVisible=true;
        VideoEditPanel2.IsVisible=true;
    }
}