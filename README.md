# ImageDrawProject
 Unity在图像上绘画
# 写在前面
项目需要，要实现在图像上进行绘画，看来网上的很多Unity绘画代码，感觉挺复杂的而且功能不全，这里我自己实现了一个在图像上进行绘画的代码，包含了涂鸦、一键填充颜色、撤销上一次操作、保存图片功能。

本项目是在[http://www.qb5200.com/article/391439.html](http://www.qb5200.com/article/391439.html)上进行了魔改。

# 效果
左键涂鸦、右键一键填充、空格撤销上一次操作、程序关闭自动保存

![在这里插入图片描述](https://img-blog.csdnimg.cn/348bc32268704f4c8a5d9ef8a2ab004a.gif)

# 项目地址

# 关键讲解
**1、图片的设置**
为了让图片可以编辑，在图片的Inspector窗口要把这两个地方改一下

![在这里插入图片描述](https://img-blog.csdnimg.cn/ad3858bfb5e94685a287edbc6e688ebb.png)

**2、我这里用的是射线检测，没有用UGUI的事件触发API所以要改一下相机和画布的设置**
相机改为正交

![在这里插入图片描述](https://img-blog.csdnimg.cn/4605efc9c28a42fda2766e7f026c1d2b.png)

画布改为屏幕空间Camera模式

![在这里插入图片描述](https://img-blog.csdnimg.cn/76e8978851ee489e8508dc02f32914a4.png)

只有这样才能进行鼠标点击的射线才能打到图像。

**3、坐标转化，如何获取鼠标点击的图片的哪个像素**

（1）用`Vector3 mouseWorldPosition =  Camera.main.ScreenToWorldPoint(Input.mousePosition)`将鼠标坐标转化为世界坐标.其中`Input.mousePosition`屏幕坐标的起点位置 左下角为(0,0)点，右上角为(Screen.width，Screen.height) 。`mouseWorldPosition`的z轴为摄像机的z轴位置。

（2）再用`Vector2 localPos = transform.InverseTransformPoint(mouseWorldPosition);`将世界坐标转化为图片的本地坐标位置，z轴直接不要。`transformA.InverseTransformPoint(transformB.position)`就是获取transfromB相对于transformA的局部坐标。
此时如果图像为(512,512)大小，则图像左下角(-256,-256)右上角(256,256)

（3）最后用将坐标转为左下角(0,0)右上角(512,512)这是为了之后便于计算
```csharp
float pixelWidth = drawSprite.rect.width;//512
float pixelHeight = drawSprite.rect.height;//512
float centeredX = localPos.x + pixelWidth / 2;
float centeredY = localPos.y + pixelHeight / 2;
//左下角(0,0)右上角(512,512)
Vector2 pixel_pos = new Vector2(Mathf.RoundToInt(centeredX), Mathf.RoundToInt(centeredY));
```
**4、获取图片像素**
核心是用`currentColorArray = drawableTexture2D.GetPixels32();`获取图像的每个点像素值，这个`GetPixels32()`方法会返回`Color32[]`的数组，如果图像是(512,512)大小，那么这是数组的大小就为512×512。数组第0个元素为图片左下角的像素，第512×512个元素为图片右上角的像素。这就是为什么我们在3中要进行坐标转化的原因，因为要一一对应。
**5、涂鸦**
在3中知道鼠标点击的是哪个像素点，4中又知道了图像的所有点像素值，找到对应点更改`Color32[]`数组中的颜色即可，改完了记得设置回图片。
```csharp
drawableTexture2D.SetPixels32(currentColorArray);
drawableTexture2D.Apply();
```
涂鸦代码详见`MarkPixelsToColour()`函数
注意一下，为了防止鼠标滑动太快导致涂鸦点间断的问题，这里使用了插值方法，根据前一个点和后一个点的的距离，叉出中间点。
```csharp
//计算当前的位置和上一次记录的位置之间的距离，然后平滑的画，这是为了防止鼠标移动太快，画的点不连续
float distance = Vector2.Distance(previousDragPosition, pixelPos);
float steps = 1 / distance;
for (float lerp = 0; lerp <= 1; lerp += steps)
{
    //插值
    Vector2 curPosition = Vector2.Lerp(previousDragPosition, pixelPos, lerp);
    //画
    PenDraw(curPosition);
}
previousDragPosition = pixelPos;
```
**6、一键填充**
用到基于栈的非递归泛洪填充算法，不要用的递归去做因为unity会报栈溢出。
泛洪填充算法详见[https://blog.csdn.net/jia20003/article/details/8908464/](https://blog.csdn.net/jia20003/article/details/8908464/) 
主要是栈来存储一个点周围可能要填充的点(进栈)。在循环里一直进栈一个点周围要填充的点，然后出栈要填充的点，再判断出栈点周围的点是否进栈。
详见`FloodFillScanLineWithStack()`函数
**7、撤销上一步**
用`Stack<Color32[]>`栈实现，就是绘画时记录一下绘画前的像素。撤回时就出栈。
**8、保存**
```csharp
byte[] bytes = drawableTexture2D.EncodeToPNG();
File.WriteAllBytes(Application.dataPath + "/SavedScreen.png", bytes);
drawableTexture2D.SetPixels32(orignalColorArray);
drawableTexture2D.Apply();
```
# 写在后面
未闻花名，但识花香，再遇花时，泪已千行。

![在这里插入图片描述](https://img-blog.csdnimg.cn/061a89093fd1489184cadedf45ec0f76.jpeg)
