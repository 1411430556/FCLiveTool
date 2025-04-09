using System.Xml.Serialization;

namespace FCLiveToolApplication.Popup;

public partial class VideoSubListPopup : CommunityToolkit.Maui.Views.Popup
{
	public VideoSubListPopup(int popupType,string vsn="")
	{
		InitializeComponent();

        ReceiveVideoSubName = vsn;
        PopupType = popupType;
	}

    public VideoSubList CurrentItem = null;
    public string ReceiveVideoSubName;
    /// <summary>
    /// 0�����½����ģ�1����༭���ж��ġ�
    /// </summary>
    public int PopupType = 0;

    private async void MainGrid_Loaded(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            VideoSubPage.videoSubPage.PopShowMsg("����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ����Ͷ�ȡ�ļ����������Ȩ�������漰�ļ���д�Ĳ������޷�����ʹ�ã�");
            this.Close();
        }

        if(PopupType==0)
        {
            SubListManagerTitle.Text="��Ӷ���";

            string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
            if (dataPath != null)
            {
                try
                {
                    if (!File.Exists(dataPath+"\\VideoSubList.log"))
                    {
                        File.Create(dataPath+"\\VideoSubList.log");
                    }

                    CurrentItem=new VideoSubList();

                }
                catch (Exception)
                {
                    VideoSubPage.videoSubPage.PopShowMsg("��ȡ��������ʱ����");
                    this.Close();
                }

            }
            else
            {
                VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                this.Close();
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ReceiveVideoSubName))
            {
                VideoSubPage.videoSubPage.PopShowMsg("�������������ԣ�");
                this.Close();
            }

            SubListManagerTitle.Text="�༭����";
            VideoSubNameTb.IsEnabled=false;

            string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
            if (dataPath != null)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
                try
                {
                    if (File.Exists(dataPath+"\\VideoSubList.log"))
                    {
                        var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                        CurrentItem = tlist.FirstOrDefault(p => p.SubName==ReceiveVideoSubName);
                        if (CurrentItem != null)
                        {
                            VideoSubNameTb.Text=CurrentItem.SubName;
                            VideoTagTb.Text=CurrentItem.SubTag;
                            VideoURLTb.Text=CurrentItem.SubURL;
                            VideoEnabledUpdate.IsToggled=CurrentItem.IsEnabledUpdate;
                            VideoUATb.Text=CurrentItem.UserAgent;
                        }
                        else
                        {
                            VideoSubPage.videoSubPage.PopShowMsg("δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�");
                            this.Close();
                        }


                    }
                    else
                    {
                        VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                        this.Close();
                    }
                }
                catch (Exception)
                {
                    VideoSubPage.videoSubPage.PopShowMsg("��ȡ��������ʱ����");
                    this.Close();
                }

            }
            else
            {
                VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                this.Close();
            }
        }

    }

    private void SubmitBtn_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(VideoSubNameTb.Text))
        {
            VideoSubPage.videoSubPage.PopShowMsg("�������Ʋ���Ϊ�գ�");
            return;
        }    
        if (string.IsNullOrWhiteSpace(VideoURLTb.Text))
        {
            VideoSubPage.videoSubPage.PopShowMsg("���ĵ�ַ����Ϊ�գ�");
            return;
        }


        if (PopupType==0)
        {
            string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
            if (dataPath != null)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
                try
                {
                    if (File.Exists(dataPath+"\\VideoSubList.log"))
                    {
                        var localStr = File.ReadAllText(dataPath+"\\VideoSubList.log");
                        List<VideoSubList> tlist = new List<VideoSubList>();
                        if(!string.IsNullOrWhiteSpace(localStr))
                        {
                            tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(localStr));
                        }
          
                        var items = tlist.FirstOrDefault(p => p.SubName==VideoSubNameTb.Text);
                        if (items != null)
                        {
                            VideoSubPage.videoSubPage.PopShowMsg("��ǰ����Ķ��������ѱ�ʹ�ã���������ƣ�");
                            return;
                        }
                        else
                        {
                            CurrentItem.SubName=VideoSubNameTb.Text;
                            CurrentItem.SubTag=VideoTagTb.Text;
                            CurrentItem.SubURL=VideoURLTb.Text;
                            CurrentItem.IsEnabledUpdate=VideoEnabledUpdate.IsToggled;
                            CurrentItem.UserAgent=VideoUATb.Text;

                            tlist.Add(CurrentItem);
                            VideoSubPage.videoSubPage.RefreshVSL(tlist);

                            using (StringWriter sw = new StringWriter())
                            {
                                xmlSerializer.Serialize(sw, tlist);
                                File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                            }

                            VideoSubPage.videoSubPage.PopShowMsg("��ӳɹ���");
                            this.Close();
                        }

                    }
                    else
                    {
                        VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                        this.Close();
                    }

                }
                catch (Exception)
                {
                    VideoSubPage.videoSubPage.PopShowMsg("��������ʱ������ˢ�����ԣ�");
                    this.Close();
                }

            }
            else
            {
                VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                this.Close();
            }
        }
        else
        {
            string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
            if (dataPath != null)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
                try
                {
                    if (File.Exists(dataPath+"\\VideoSubList.log"))
                    {
                        var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                        var items = tlist.FirstOrDefault(p => p.SubName==ReceiveVideoSubName);
                        if (items != null)
                        {
                            items.SubTag=VideoTagTb.Text;
                            items.SubURL=VideoURLTb.Text;
                            items.IsEnabledUpdate=VideoEnabledUpdate.IsToggled;
                            items.UserAgent=VideoUATb.Text;

                            VideoSubPage.videoSubPage.RefreshVSL(tlist);

                            using (StringWriter sw = new StringWriter())
                            {
                                xmlSerializer.Serialize(sw, tlist);
                                File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                            }

                            VideoSubPage.videoSubPage.PopShowMsg("�޸ĳɹ���");
                            this.Close();
                        }
                        else
                        {
                            VideoSubPage.videoSubPage.PopShowMsg("δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�");
                            this.Close();
                        }

                    }
                    else
                    {
                        VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                        this.Close();
                    }
                }
                catch (Exception)
                {
                    VideoSubPage.videoSubPage.PopShowMsg("��������ʱ������ˢ�����ԣ�");
                    this.Close();
                }

            }
            else
            {
                VideoSubPage.videoSubPage.PopShowMsg("Դ�ļ���ʧ�������´�����");
                this.Close();
            }
        }

    }

    private async void CancelBtn_Clicked(object sender, EventArgs e)
    {
        if (await VideoSubPage.videoSubPage.PopShowMsgAndReturn("��Ҫȡ��������"))
        {
            this.Close();
        }
    }
}