﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

// ReSharper disable CheckNamespace
namespace ShapeCrawler
{
    /// <summary>
    ///     Represents an image model.
    /// </summary>
    public class SCImage // TODO: make internal?
    {
        private readonly SCPresentation parentPresentation;
        private readonly IRemovable imageContainer;
        private readonly StringValue picReference;
        private readonly OpenXmlPart openXmlPart;
        private byte[]? bytes;

        internal SCImage(
            ImagePart imagePart,
            IRemovable imageContainer,
            StringValue picReference,
            OpenXmlPart openXmlPart)
        {
            this.ImagePart = imagePart;
            this.imageContainer = imageContainer;
            this.picReference = picReference;
            this.openXmlPart = openXmlPart;
            this.parentPresentation = ((IPresentationComponent)imageContainer).PresentationInternal;
            this.MIME = this.ImagePart.ContentType;
        }

        public string MIME { get; }

        internal ImagePart ImagePart { get; private set; }

#if NET5_0

        /// <summary>
        ///     Gets bytes content.
        /// </summary>
        public async ValueTask<byte[]> GetBytes()
        {
            var stream = this.ImagePart.GetStream();
            this.bytes = new byte[stream.Length];
            await stream.ReadAsync(this.bytes.AsMemory(0, (int)stream.Length)).ConfigureAwait(false);

            stream.Close();
            return this.bytes;
        }

#else
        public async Task<byte[]> GetBytes()
        {
            if (bytes != null)
            {
                return bytes; // return from cache
            }

            Stream stream = this.ImagePart.GetStream();
            bytes = new byte[stream.Length];
            await stream.ReadAsync(bytes, 0, (int) stream.Length).ConfigureAwait(false);
            stream.Close();
            return bytes;
        }
#endif

        /// <summary>
        ///     Sets image with stream.
        /// </summary>
        public void SetImage(Stream sourceStream)
        {
            this.imageContainer.ThrowIfRemoved();

            bool isSharedImagePart = this.parentPresentation.ImageParts.Count(ip => ip == this.ImagePart) > 1;
            if (isSharedImagePart)
            {
                string rId = $"rId{Guid.NewGuid().ToString().Substring(0,5)}";
                this.ImagePart = this.openXmlPart.AddNewPart<ImagePart>("image/png", rId);
                this.picReference.Value = rId;
            }

            sourceStream.Position = 0;
            this.ImagePart.FeedData(sourceStream);
            this.bytes = null; // resets cache
        }

        /// <summary>
        ///     Sets image with byte array.
        /// </summary>
        public void SetImage(byte[] sourceBytes)
        {
            var stream = new MemoryStream();
            stream.Write(sourceBytes, 0, sourceBytes.Length);
            this.SetImage(stream);
        }

#if NETSTANDARD2_0
        public void SetImage(string filePath)
        {
            byte[] sourceBytes = File.ReadAllBytes(filePath);
            this.SetImage(sourceBytes);
        }
#else

        /// <summary>
        ///     Sets image from file contents.
        /// </summary>
        public async Task SetImage(string filePath)
        {
            byte[] sourceBytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            this.SetImage(sourceBytes);
        }
#endif

        internal static SCImage CreatePictureImage(Shape picture, OpenXmlPart openXmlPart, StringValue picReference)
        {
            var imagePart = (ImagePart)openXmlPart.GetPartById(picReference.Value);

            return new SCImage(imagePart, picture, picReference, openXmlPart);
        }

        internal static SCImage GetSlideBackgroundImageOrDefault(SCSlide parentSlide)
        {
            P.Background pBackground = parentSlide.SDKSlidePart.Slide.CommonSlideData.Background;
            if (pBackground == null)
            {
                return null;
            }

            A.BlipFill aBlipFill = pBackground.Descendants<A.BlipFill>().SingleOrDefault();
            StringValue picReference = aBlipFill?.Blip?.Embed;
            if (picReference == null)
            {
                return null;
            }

            var imagePart = (ImagePart)parentSlide.SDKSlidePart.GetPartById(picReference.Value);
            var backgroundImage = new SCImage(imagePart, parentSlide, picReference, parentSlide.SDKSlidePart);

            return backgroundImage;
        }

        internal static SCImage GetFillImageOrDefault(Shape parentShape, SlidePart slidePart, OpenXmlCompositeElement compositeElement)
        {
            P.Shape pShape = (P.Shape)compositeElement;
            A.BlipFill aBlipFill = pShape.ShapeProperties.GetFirstChild<A.BlipFill>();

            StringValue picReference = aBlipFill?.Blip?.Embed;
            if (picReference == null)
            {
                return null;
            }

            var imagePart = (ImagePart)slidePart.GetPartById(picReference.Value);

            return new SCImage(imagePart, parentShape, picReference, slidePart);
        }
    }
}