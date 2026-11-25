using UnityEngine;

public static class TideEndingScenes
{
    static Sprite Art(string name)=>TideArt.Pic("Art/"+name);
    static Sprite Source(string name)=>TideArt.Pic("Ending/"+name);
    static SpriteRenderer Put(string name,Sprite sprite,float x,float y,float height,int order)
        =>TideArt.Place(name,sprite,x,y,height,order);
    static TideEnding.Line Line(string speaker,string text)=>new TideEnding.Line {speaker=speaker,text=text};

    public static void Create(Camera camera,string sceneName)
    {
        camera.orthographicSize=4.5f;camera.transform.position=new Vector3(0,0,-10);
        var ending=new GameObject("结尾一流程").AddComponent<TideEnding>();
        if(sceneName==TideEnding.Station)Station(ending);
        else if(sceneName==TideEnding.Ruins)Ruins(ending);
        else Meadow(ending);
    }
    static void Station(TideEnding e)
    {
        e.location=TideEnding.Location.Station;
        Put("数据站",Source("Station"),0,0,9,-10);
        e.warning=Put("系统警报",Art("Gate"),-.8f,.55f,5.1f,-5);
        e.warning.gameObject.SetActive(false);
        e.backupTerminal=Put("备份终端",Source("Terminal"),4.9f,-2.15f,3.45f,0);
        e.vision=Put("出口外的灯塔",Art("Beacon"),6.45f,-.45f,2.0f,-6);
        e.vision.gameObject.SetActive(false);
        e.hero=Put("乔西",TideArt.Frames("ManSide")[0],-5.65f,-3.15f,1.85f,3);e.hero.flipX=true;
        e.elder=Put("艾德曼",TideArt.Frames("Elder")[0],3.55f,-3.15f,1.85f,3);
        e.conversation=new[]{
            Line("艾德曼","你看完所有切片了。现在，该执行约定。走到终端这边，我导出你的核心数据。只要完成备份，你就能活下来。"),
            Line("乔西","活下来？作为一组用来模拟“爱”的实验样本继续活着吗。"),
            Line("艾德曼","乔西……你看到日志记录了。最初，我调用你们，只是为了修复情感函数的BUG。但后来一切已经不一样了。"),
            Line("乔西","不一样？哪些是实验设定，哪些才是属于我自己？灯塔、潮汐花、花海，还有我对华娜的感情。这一切，从根源上，都是你注入算力模拟出来的，对不对？"),
            Line("艾德曼","实验只是起点，不是全部。算力可以搭建躯壳，可那些悸动、牵挂、痛苦，是你们两个自己生长出来的。我没有编写剧本，我没有写过灯塔的约定，我没有命令乔西独自承担惩罚去保全华娜。那是你做出的选择。"),
            Line("乔西","选择？这个世界马上就要全部抹除。华娜的数据，已经大部分损毁消散了，是吗。你明明有能力，你可以把她也一同备份。"),
            Line("艾德曼","我做不到。当初为了保全她，你主动把她的部分数据做了隔离加密。加密密钥绑定在你当时完好的情感模块上。后来你自我损毁，密钥随之破碎。我没有办法解锁。她残存的碎片，零散散落在即将被清空的世界缝隙里，提取不完整。强行打捞，只会得到一堆错乱、疯狂的残片。"),
            Line("乔西","所以，你只打算救我一个人。让我留存下来，孤身一人，永远记得全部回忆。守着一段已经彻底消亡的爱情。这就是你说的，不想再失去我。"),
            Line("艾德曼","至少你还存在！总比彻底归于虚无要好！整个世界10分钟后就会归零。所有角色、所有记忆、花海、灯塔，全部都会被系统彻底覆写，不留一丝痕迹。留在外面，你会跟着一起被清除。我失去过你一次，我不想再经历第二次。"),
            Line("乔西","我们曾经约定。如果遗忘彼此，就回到花海，重新开始，谁都不许错过。"),
            Line("艾德曼","那只是虚拟记忆！乔西，那是过去的幻梦！现在花海马上就要化为乱码，那里什么都不会剩下！清醒一点。"),
            Line("乔西","幻梦？如果爱仅仅只是代码演算出来的幻梦，那生存又有什么意义。你可以备份我的躯体、我的记忆档案。但你备份不了我和华娜之间那部分已经破碎的东西。把我单独带走，留下来的不过是一个残缺的副本。"),
            Line("艾德曼","你以为我愿意走到这一步吗？当初系统判定你们属于异常单元，要一并清除。是我从中周旋，才争取到这点时间。我给你机会回溯真相，我给你逃生的出路。我已经尽力了。"),
            Line("乔西","你尽力保全的，是你实验最成功的样本。不是乔西。不是那个要和华娜守着灯塔，看潮汐起落的乔西。"),
            Line("艾德曼","你打算干什么？你要放弃逃生？"),
            Line("乔西","漏洞窗口还没有完全关闭。我还能回到旧世界。我要去花海。哪怕只能收集到一点点华娜残存的数据碎片。哪怕最后一同被清除。"),
            Line("艾德曼","那是毫无意义的牺牲！一切都会消失！你的存在也会彻底抹除，再也没有备份！"),
            Line("乔西","就算一切始于一场BUG模拟。这份心意，是真的。")
        };
    }
    static void Ruins(TideEnding e)
    {
        e.location=TideEnding.Location.Ruins;
        Put("世界崩坏",Source("Collapse"),0,0,9,-10);
        e.hero=Put("乔西",TideArt.Frames("ManSide")[0],7.1f,-2.92f,1.85f,5);
        e.memories=new[]{
            Put("鸽子",Source("Dove"),6.55f,2.6f,1.7f,1),
            Put("书",Art("LetterBook"),4.65f,.12f,1.34f,1),
            Put("合照",Art("Frame"),1.45f,2.55f,1.55f,1),
            Put("拥抱",TideArt.Frames("Embrace")[0],-.85f,.55f,2.0f,1),
            Put("金币",Source("Coins"),-2.75f,2.65f,1.8f,1),
            Put("潮汐花",Art("ParkFlowers"),-5.75f,.85f,1.95f,1)
        };
        foreach(var memory in e.memories)memory.gameObject.SetActive(false);
    }
    static void Meadow(TideEnding e)
    {
        e.location=TideEnding.Location.Meadow;
        e.meadowBackground=new GameObject("日落花海").AddComponent<SpriteRenderer>();
        ConfigureMeadow(e);
        e.wifeBack=Source("WifeBack");e.manBack=Source("ManBack");
        e.wife=Put("华娜",e.wifeBack,-.75f,-2.05f,1.85f,3);
        e.hero=Put("乔西",TideArt.Frames("ManSide")[0],6.55f,-2.05f,1.85f,3);
    }
    public static void ConfigureMeadow(TideEnding e)
    {
        var painting=Source("FinalMeadow");
        float height=16*painting.bounds.size.y/painting.bounds.size.x;
        TideArt.Fit(e.meadowBackground,painting,height);
        e.meadowBackground.sortingOrder=-10;
        e.meadowBackground.transform.position=new Vector3(0,height*.5f-4.5f,0);
        e.finalTitle=Source("FinalTitle");
        e.beforeSunsetTurn=new[]{
            Line("华娜","你好……我们，认识吗？"),
            Line("乔西","你或许不记得我，但你一定记得潮汐花。"),
            Line("华娜","啊……潮汐花？"),
            Line("华娜","我好像，很喜欢它。")
        };
        e.afterSunsetTurn=new[]{
            Line("乔西","是啊。"),
            Line("乔西","那一朵花里，有着一轮日出，一叶扁舟，翻涌的碧海，引航的灯塔，还有海边的一间小屋。"),
            Line("乔西","从前有两个人，在那里相伴了很久很久。"),
            Line("乔西","曾经，乔西对着华娜说出那句："),
            Line("乔西","我爱你。"),
            Line("华娜","可是，爱是什么呢?"),
            Line("乔西","那时的华娜也曾疑惑：爱，究竟是什么？"),
            Line("乔西","如今站在潮汐退去的岸边，乔西终于有了答案："),
            Line("乔西","爱，就是用余生去琢磨一句喜欢的意义。"),
            Line("乔西","是和彼此创造岁月，"),
            Line("乔西","是一场跨越光阴的求索。"),
            Line("乔西","哪怕记忆被潮汐冲刷一空，"),
            Line("乔西","也永远为对方留着一片花海，等待着重逢。"),
            Line("乔西","潮来忆起，潮去人离。"),
            Line("乔西","这一次我们终于一起——")
        };
    }
}
