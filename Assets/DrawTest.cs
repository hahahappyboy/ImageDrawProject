
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawTest : MonoBehaviour
{
    //笔的颜色
    public static Color32 penColour = Color.red;
    //笔的宽度
    public static int penWidth = 3;
    //画的图
    private Sprite drawSprite;
    //画的纹理
    private Texture2D drawableTexture2D;
    //之前画的位置
    private Vector2 previousDragPosition = Vector2.zero;
    //存放最初的颜色数组
    private Color32[] orignalColorArray;
    //存放目前的颜色数组
    private Color32[] currentColorArray;
    //存放画的前一张图，用于撤回
    private Color32[] previousColorArray;
    //图像的宽高
    private int spriteHeight;
    private int spriteWidth;
    //当前是否在滑动
    private bool isDraging = false;
    //保存画过的点
    private Stack<Color32[]> savePixelStack;
    private void Awake() {
        savePixelStack = new Stack<Color32[]>();
        drawSprite = GetComponent<Image>().sprite;
        drawableTexture2D = drawSprite.texture;
        orignalColorArray = drawableTexture2D.GetPixels32();
        //当前sprite各个像素的颜色
        currentColorArray = drawableTexture2D.GetPixels32();
        
        spriteHeight = (int)drawSprite.rect.height;
        spriteWidth = (int)drawSprite.rect.width;
        
    }
    
    void Update()
    {   
         //画图
         bool mouseHeldDown = Input.GetMouseButton(0);
         if (mouseHeldDown) {
             //Input.mousePosition屏幕坐标的起点位置 左下角为（0，0）点，右上角为（Screen.width，Screen.height） 
             //Camera.main.ScreenToWorldPoint 转换到摄像机平面距离为 z 的屏幕空间点创建的世界空间点。
             Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
             RaycastHit2D  hit = Physics2D.Raycast(mouseWorldPosition,Vector2.zero);
             if (hit.collider != null) {
                 if (isDraging==false) {//刚按下鼠标左键，还没拖动
                     previousColorArray = drawableTexture2D.GetPixels32();
                 }
                 //正在滑动
                 isDraging = true;
                 //鼠标位置转化为对应sprite像素位置        
                 Vector2 pixelPos = WorldToPixelCoordinates(mouseWorldPosition);
                 //如果是0,0点就说明是重新开始画，就不用从之前的点lerp了
                 if (previousDragPosition == Vector2.zero) {
                     previousDragPosition = pixelPos;
                 }
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
                 
                 drawableTexture2D.SetPixels32(currentColorArray);
                 drawableTexture2D.Apply();
             }
         }else {
             //鼠标没有按住
             previousDragPosition = Vector2.zero;
         } 
         
         //一键变色
         bool mouseDown1 = Input.GetMouseButtonDown(1);
         if (mouseDown1) {
             Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
             RaycastHit2D  hit = Physics2D.Raycast(mouseWorldPosition,Vector2.zero);
             if (hit.collider != null) {
                 //正在滑动
                 isDraging = true;
                 //鼠标位置转化为对应sprite像素位置        
                 Vector2 pixelPos = WorldToPixelCoordinates(mouseWorldPosition);
                 //中心位置X
                 int centerX = (int)pixelPos.x;//363
                 //中心位置Y
                 int centerY = (int)pixelPos.y;//396
                 int centerPos = centerY * (int)spriteWidth + centerX;
                 Color32 centerPosColor = currentColorArray[centerPos];
                 FloodFillScanLineWithStack(centerX, centerY, penColour, centerPosColor);
                 previousColorArray = drawableTexture2D.GetPixels32();
                 drawableTexture2D.SetPixels32(currentColorArray);
                 drawableTexture2D.Apply();  
             }
         }

         if ((Input.GetMouseButtonUp(0)||Input.GetMouseButtonUp(1)) && isDraging) {
             isDraging = false;
             Debug.Log("放入");
             savePixelStack.Push(previousColorArray);
             
         }

         if (Input.GetKeyDown(KeyCode.Space)) {
             if (savePixelStack.Count>0) {
                 Debug.Log("撤销");
                 currentColorArray = savePixelStack.Pop();
                 drawableTexture2D.SetPixels32(currentColorArray);
                 drawableTexture2D.Apply();
             }
         }
        
    }

    
    # region 画图
    /// <summary>
    /// 在图形上画
    /// </summary>
    /// <param name="pixelPos"></param>
    private void PenDraw(Vector2 pixelPos) {
      
        MarkPixelsToColour(pixelPos, penWidth);
       
    }
    /// <summary>
    /// 在颜色数组中找到点击的像素的位置，并更改颜色
    /// </summary>
    /// <param name="centerPixel"></param>
    /// <param name="penWidth"></param>
    private void MarkPixelsToColour(Vector2 centerPixel, int penWidth)
    {
        //中心位置X
        int centerX = (int)centerPixel.x;//363
        //中心位置Y
        int centerY = (int)centerPixel.y;//396
        //X = 360 X<=366
        for (int x = centerX - penWidth; x <= centerX + penWidth; x++)
        {
            // y 393->399
            for (int y = centerY - penWidth; y <= centerY + penWidth; y++)
            {
                //边界外不画
                if (x >= spriteWidth || x < 0 ||
                    y>= spriteHeight|| y <0 )
                    continue;
                int arrayPos = y * (int)drawSprite.rect.width + x;
                currentColorArray[arrayPos] = penColour;
                
            }
        }
    }
    

    /// <summary>
    /// 将鼠标的世界坐标转化为图片的本地坐标左下角(-256,-256)右上角(256,256)
    /// </summary>
    /// <param name="mouseWorldPosition"></param>
    /// <returns></returns>
    private Vector2 WorldToPixelCoordinates(Vector3 mouseWorldPosition) {
        //将位置从世界空间转换为局部空间。
        //transformA.InverseTransformPoint(transformB.position),获取transfromB相对于transformA的局部坐标
        Vector2 localPos = transform.InverseTransformPoint(mouseWorldPosition);
        //localPos 左下角(-256,-256)右上角(256,256) 右下角(256,-256)
        float pixelWidth = drawSprite.rect.width;//512
        float pixelHeight = drawSprite.rect.height;//512

        float centeredX = localPos.x + pixelWidth / 2;
        float centeredY = localPos.y + pixelHeight / 2;
        //左下角(0,0)右上角(512,512)
        Vector2 pixel_pos = new Vector2(Mathf.RoundToInt(centeredX), Mathf.RoundToInt(centeredY));
        // Debug.Log(pixel_pos);
        return pixel_pos;
    }
    # endregion
    
     # region 基于栈的非递归泛洪填充算法

    private Stack<int> xStack = new Stack<int>();
    private Stack<int> yStack = new Stack<int>();
    public void FloodFillScanLineWithStack(int x, int y, Color32 newColor, Color32 oldColor) {
        //颜色相等什么也不做
        if(oldColor.Equals(newColor)) return;
        xStack.Clear();
        yStack.Clear();
        int y1; 
        bool spanLeft, spanRight;
        
        xStack.Push(x);
        yStack.Push(y);

        while (xStack.Count!=0) {
            if (xStack.Count==0) {
                return;
            } 
            x = xStack.Pop();
            y = yStack.Pop();
            //找到y这条线的底部
            y1 = y;
            while(y1 >= 0 && GetColor(x, y1).Equals(oldColor)) y1--;
            y1++;
            spanLeft = spanRight = false;
           
            while (y1 < spriteHeight && GetColor(x, y1).Equals(oldColor)) {
                //从y底部到顶部变色
                SetColor(x, y1, newColor);
                //左边是不是oldColor，是的话进栈
                if (!spanLeft && x > 0 && GetColor(x - 1, y1).Equals(oldColor)) {
                    xStack.Push(x-1);
                    yStack.Push(y1);
                    spanLeft = true;//设为true表示这个x-1, y1点是要填充的点
                }else if (spanLeft && x > 0 && !GetColor(x - 1, y1).Equals(oldColor)) {//因为x-1, y1点是要填充的点，如果x-1, y1+1点还是要填充的点旧不必放入栈了，因为x-1, y1点出栈时一定会遍历到该点
                    spanLeft = false;
                }
                //右边是不是oldColor，是的话进栈
                if(!spanRight && x < spriteWidth - 1 && GetColor(x + 1, y1).Equals(oldColor))
                {
                    xStack.Push(x+1);
                    yStack.Push(y1);
                    spanRight = true;
                }else if(spanRight && x < spriteWidth - 1 && !GetColor(x + 1, y1).Equals(oldColor))
                {
                    spanRight = false;
                } 
                y1++;
            }
        }

    }
    public Color32 GetColor(int x, int y)
    {
        int arrayPos = y * (int)spriteWidth + x;
        return currentColorArray[arrayPos];
    }
    public void SetColor(int x, int y, Color32 newColor)
    {
        int arrayPos = y * (int)spriteWidth + x;
        currentColorArray[arrayPos] = newColor;
    }

    # endregion
    
    
    
    /// <summary>
    /// 销毁前恢复为原来的图像
    /// </summary>
    protected void OnDestroy()
    {
        drawableTexture2D.SetPixels32(currentColorArray);
        drawableTexture2D.Apply();
        byte[] bytes = drawableTexture2D.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/SavedScreen.png", bytes);
        drawableTexture2D.SetPixels32(orignalColorArray);
        drawableTexture2D.Apply();
    }
    
    
    
   
}
