﻿using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using KegID.Common;
using KegID.Android.Renderers;

[assembly: ExportRenderer(typeof(SignaturePad), typeof(SignaturePadRenderer))]
namespace KegID.Android.Renderers
{
    public class SignaturePadRenderer : ViewRenderer<SignaturePad, FrameLayout>
    {
        private Paint paint = new Paint();
        private global::Android.Graphics.Path path = new global::Android.Graphics.Path();
        private float previousX;
        private float previouxY;

        public SignaturePadRenderer(Context context) : base(context)
        {
            paint.AntiAlias = true;
            paint.SetStyle(Paint.Style.Stroke);
            paint.Color = SignaturePad.DefaultStrokeColor.ToAndroid();
            paint.StrokeJoin = Paint.Join.Round;
            paint.StrokeCap = Paint.Cap.Round;
            paint.StrokeWidth = SignaturePad.DefaultStrokeWidth * context.Resources.DisplayMetrics.Density;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<SignaturePad> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                e.OldElement.ImageStreamRequested -= OnImageStreamRequested;
            }

            if (e.NewElement != null)
            {
                e.NewElement.ImageStreamRequested += OnImageStreamRequested;
            }

            Background = new ColorDrawable(SignaturePad.DefaultBackgroundColor.ToAndroid());
        }

        protected override void OnDraw(Canvas canvas)
        {
            canvas.DrawPath(path, paint);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            float currentX = e.GetX();
            float currentY = e.GetY();

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    path.MoveTo(currentX, currentY);

                    previousX = currentX;
                    previouxY = currentY;
                    return true;

                case MotionEventActions.Move:
                case MotionEventActions.Up:
                    for (int i = 0; i < e.HistorySize; i++)
                    {
                        float historicalX = e.GetHistoricalX(i);
                        float historicalY = e.GetHistoricalY(i);
                        path.LineTo(historicalX, historicalY);
                    }
                    path.LineTo(currentX, currentY);

                    Invalidate();

                    previousX = currentX;
                    previouxY = currentY;
                    return true;

                default:
                    return false;
            }
        }

        private void OnImageStreamRequested(object sender, SignaturePad.ImageStreamRequestedEventArgs e)
        {
            e.ImageStream = CreateImageStream(e.PrintWidth);
        }

        private Stream CreateImageStream(double printWidth)
        {
            using (Bitmap image = CreateSignatureBitmap(printWidth))
            {
                MemoryStream stream = new MemoryStream();
                bool success = image.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

                image.Recycle();

                if (success)
                {
                    stream.Position = 0;
                    return stream;
                }
            }

            return null;
        }

        private Bitmap CreateSignatureBitmap(double printWidth)
        {
            float width = (float)Element.Bounds.Width * Resources.DisplayMetrics.Density;
            float height = (float)Element.Bounds.Height * Resources.DisplayMetrics.Density;
            float scale = (float)printWidth / width;
            float printHeight = height * scale;

            Bitmap bitmap = Bitmap.CreateBitmap((int)width, (int)height, Bitmap.Config.Argb8888);
            using (Canvas canvas = new Canvas(bitmap))
            {
                canvas.DrawColor(global::Android.Graphics.Color.White);
                canvas.DrawPath(path, paint);
            }

            return Bitmap.CreateScaledBitmap(bitmap, (int)printWidth, (int)printHeight, false);
        }
    }
}