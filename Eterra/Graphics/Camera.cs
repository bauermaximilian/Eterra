﻿/* 
 * Eterra Framework
 * A simple framework for creating multimedia applications.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Eterra.Common;
using System;
using System.Numerics;

namespace Eterra.Graphics
{
    /// <summary>
    /// Defines the supported projection modes.
    /// </summary>
    public enum ProjectionMode
    {
        /// <summary>
        /// A perspective projection, where distant objects appear smaller
        /// than nearer objects - just like in the real world.
        /// Suitable for drawing realistic 3D scenes.
        /// In this mode, a vertex with the position 
        /// <see cref="Vector3.UnitZ"/> would be displayed at the very 
        /// center of the screen (assuming the default camera position and
        /// near/far clipping).
        /// </summary>
        Perspective,
        /// <summary>
        /// An orthographic projection, which is a parallel projection where 
        /// the distance of the drawn objects to the camera doesn't affect 
        /// their size.
        /// Suitable for drawing two-dimensional/isometric scenes.
        /// In this mode, a vertex with the position 
        /// <see cref="Vector3.UnitZ"/> would be displayed at the bottom
        /// left corner of the render target (window, screen or
        /// <see cref="RenderTextureBuffer"/>).
        /// <see cref="Vector3.UnitY"/> is equal to the complete height of the 
        /// render target and <see cref="Vector3.UnitX"/> is equal to the 
        /// complete width of the render target. To take the aspect ratio of 
        /// the render target into consideration and maintain quadratic pixels,
        /// use <see cref="OrthgraphicRelativeProportional"/>.
        /// </summary>
        OrthographicRelative,
        /// <summary>
        /// An orthographic projection, which is a parallel projection where 
        /// the distance of the drawn objects to the camera doesn't affect 
        /// their size.
        /// Suitable for drawing two-dimensional/isometric scenes.
        /// In this mode, a vertex with the position 
        /// <see cref="Vector3.UnitZ"/> would be displayed at the bottom
        /// left corner of the render target (window, screen or
        /// <see cref="RenderTextureBuffer"/>).
        /// <see cref="Vector3.UnitY"/> and <see cref="Vector3.UnitX"/> are 
        /// both equal to the smaller side of the render target, the longer 
        /// side is equal to the aspect ratio. This takes the aspect ratio of 
        /// the render target into consideration and preserves square pixels, 
        /// but causes the mentioned "overhang" at the longer side of the 
        /// render target. See the examples for more information.
        /// </summary>
        /// <example>
        /// A ratio of 16/9 =~ 1.78 means the width is greater than the
        /// height and therefore causes an total surplus of (1.78 - 1) = 0.78,
        /// which is 0.78/2 = 0.39 on both the left and right side; a plane
        /// which should completely fill the screen should therefore have the
        /// following dimensions: X = -0.39, Y = 0, Width = 1.78, Height = 1.
        /// A ratio of 9/16 = 0.5625 would have a similar effect, just that
        /// the smaller side is now the width (with a length of 1) and the
        /// height has a total surplus of [...]
        /// </example>
        OrthgraphicRelativeProportional,//OrthographicRelativeSmallestSide
        /// <summary>
        /// An orthographic projection, which is a parallel projection where 
        /// the distance of the drawn objects to the camera doesn't affect 
        /// their size. 
        /// Suitable for drawing two-dimensional/isometric scenes.
        /// In this unit, one world unit equals one pixel drawn on the screen;
        /// the world origin is located at 
        /// <c>X=0/Y=<see cref="IGraphics.Height"/></c> in screen space.
        /// </summary>
        OrthographicAbsolute
    }

    /// <summary>
    /// Represents a camera in three-dimensional space.
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// Defines the minimum value for each component of
        /// <see cref="ClippingRange"/>.
        /// </summary>
        public const float ClippingMinimum = 0.001f;

        /// <summary>
        /// Defines the minimum value for each component of
        /// <see cref="ClippingRange"/>.
        /// </summary>
        public const float ClippingMaximum = float.MaxValue / 2;

        /// <summary>
        /// Gets the default position of the <see cref="Camera"/> 
        /// upon initialisation.
        /// </summary>
        public static Vector3 DefaultPosition { get; }
            = Vector3.UnitZ * -5;

        /// <summary>
        /// Gets the position of the camera.
        /// The initial value is <see cref="DefaultPosition"/>.
        /// </summary>
        public Vector3 Position { get; set; } = DefaultPosition;

        /// <summary>
        /// Gets the rotation of the camera as <see cref="Quaternion"/>.
        /// The initial value is <see cref="Quaternion.Identity"/>.
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Gets or sets the projection mode of the camera.
        /// The initial value is <see cref="ProjectionMode.Perspective"/>.
        /// </summary>
        public ProjectionMode ProjectionMode
        {
            get => projection;
            set
            {
                if (!Enum.IsDefined(typeof(ProjectionMode), projection))
                    throw new ArgumentException("The specified projection " +
                        "mode is invalid.");
                else projection = value;
            }
        }
        private ProjectionMode projection =
            ProjectionMode.Perspective;

        /// <summary>
        /// Gets or sets the range in which polygons are drawn. All polygons
        /// outside that range (closer to <see cref="Position"/> than the 
        /// lowest component value of <see cref="ClippingRange"/> or further 
        /// afar to <see cref="Position"/> than the largest component value 
        /// of <see cref="ClippingRange"/>) will be clipped and not drawn.
        /// The values are automatically brought in the right order and
        /// valid range defined by <see cref="ClippingMinimum"/> and
        /// <see cref="ClippingMaximum"/>. Mind that extremely high or low 
        /// values might cause precision-caused artifacts on some graphics
        /// devices. The default value is (0.25|1000).
        /// </summary>
        public Vector2 ClippingRange
        {
            get => clippingRange;
            set
            {
                float lower = Math.Max(Math.Min(value.X, value.Y),
                    ClippingMinimum);
                float upper = Math.Min(Math.Max(value.X, value.Y),
                    ClippingMaximum);
                clippingRange = new Vector2(lower, upper);
            }
        }
        private Vector2 clippingRange = new Vector2(0.25f, 1000);

        /// <summary>
        /// Gets or sets the field of view, which is (only) used when 
        /// <see cref="ProjectionMode"/> is 
        /// <see cref="ProjectionMode.Perspective"/>.
        /// The default value is 70°.
        /// </summary>
        public Angle PerspectiveFieldOfView
        {
            get => fieldOfView;
            set { fieldOfView = value.ToNormalized(); }
        }
        private Angle fieldOfView = Angle.Deg(70, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera"/> class with
        /// default values for the <see cref="ProjectionMode"/> 
        /// "<see cref="ProjectionMode.Perspective"/>".
        /// </summary>
        public Camera() { }

        /// <summary>
        /// Rotates the current <see cref="Camera"/> instance.
        /// </summary>
        /// <param name="deltaEulerX">
        /// The amount of euler rotation around the X axis.
        /// </param>
        /// <param name="deltaEulerY">
        /// The amount of euler rotation around the Y axis.
        /// </param>
        public void Rotate(Angle deltaEulerX, Angle deltaEulerY)
        {
            Rotate(deltaEulerX, deltaEulerY, 0);
        }

        /// <summary>
        /// Rotates the current <see cref="Camera"/> instance.
        /// </summary>
        /// <param name="deltaEulerX">
        /// The amount of euler rotation around the X axis.
        /// </param>
        /// <param name="deltaEulerY">
        /// The amount of euler rotation around the Y axis.
        /// </param>
        /// <param name="deltaEulerZ">
        /// The amount of euler rotation around the Z axis.
        /// </param>
        public void Rotate(Angle deltaEulerX, Angle deltaEulerY, 
            Angle deltaEulerZ)
        {
            Rotate(new Vector3(deltaEulerX, deltaEulerY, deltaEulerZ));
        }

        /// <summary>
        /// Rotates the current <see cref="Camera"/> instance.
        /// </summary>
        /// <param name="deltaEuler">
        /// The amount of euler rotation around the X, Y and Z axis in radians.
        /// </param>
        public void Rotate(Vector3 deltaEuler)
        {
            Rotation = MathHelper.CombineRotation(Rotation, deltaEuler);
        }

        /// <summary>
        /// Rotates the current <see cref="Camera"/> instance.
        /// </summary>
        /// <param name="eulerY">
        /// The new euler rotation value of the X axis.
        /// </param>
        /// <param name="eulerX">
        /// The new euler rotation value of the Y axis.
        /// </param>
        /// <param name="eulerZ">
        /// The new euler rotation value of the Z axis.
        /// </param>
        public void RotateTo(Angle eulerX, Angle eulerY, Angle eulerZ)
        {
            RotateTo(new Vector3(eulerX, eulerY, eulerZ));
        }

        /// <summary>
        /// Rotates the current <see cref="Camera"/> instance.
        /// </summary>
        /// <param name="eulerRotation">
        /// The new euler rotation value of the X, Y and Z axis.
        /// </param>
        public void RotateTo(Vector3 eulerRotation)
        {
            Rotation = MathHelper.CreateRotation(eulerRotation);
        }

        /// <summary>
        /// Moves the current <see cref="Camera"/>.
        /// </summary>
        /// <param name="newPosition">
        /// The new position of the camera.
        /// </param>
        public void MoveTo(Vector3 newPosition)
        {
            Position = newPosition;
        }

        /// <summary>
        /// Moves the current <see cref="Camera"/>.
        /// </summary>
        /// <param name="x">
        /// The X component of the new camera position.
        /// </param>
        /// <param name="y">
        /// The Y component of the new camera position.
        /// </param>
        /// <param name="z">
        /// The Z component of the new camera position.
        /// </param>
        public void MoveTo(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }

#if ENABLE_EXPERIMENTAL_API
        /// <summary>
        /// Moves the current <see cref="Camera"/>.
        /// </summary>
        /// <param name="translation">
        /// The translation of the camera.
        /// </param>
        /// <param name="translateAlongCameraAxis">
        /// <c>true</c> to translate the camera along its current axis 
        /// (rotated by the current <see cref="Rotation"/>),
        /// <c>false</c> to translate the camera without considering its
        /// current <see cref="Rotation"/>.
        /// </param>
        public void Move(Vector3 translation,
            bool translateAlongCameraAxis)
        {
            if (translateAlongCameraAxis)
            {
                Vector3 transformedTranslation = Vector3.Transform(
                    new Vector3(translation.X, 0, translation.Z), Rotation);

                //TODO: Finally make that this actually works... 
                //Ugh. I hate maths so much.
                int signZ = translation.Z >= 0 ? 1 : -1;
                int yFactor = transformedTranslation.Z >= 0 ? signZ : -signZ;

                Position = new Vector3(Position.X + transformedTranslation.X,
                    Position.Y + transformedTranslation.Y * yFactor
                    + translation.Y, Position.Z +
                    transformedTranslation.Z);
            }
            else Position += translation;
        }

        /// <summary>
        /// Moves the current <see cref="Camera"/>.
        /// </summary>
        /// <param name="translationX">
        /// The X component of the camera translation vector.
        /// </param>
        /// <param name="translationY">
        /// The Y component of the camera translation vector.
        /// </param>
        /// <param name="translationZ">
        /// The Z component of the camera translation vector.
        /// </param>
        /// <param name="translateAlongCameraAxis">
        /// <c>true</c> to translate the camera along its current axis 
        /// (rotated by the current <see cref="Rotation"/>),
        /// <c>false</c> to translate the camera without considering its
        /// current <see cref="Rotation"/>.
        /// </param>
        public void Move(float translationX, float translationY, 
            float translationZ, bool translateAlongCameraAxis)
        {
            Move(new Vector3(translationX, translationY, translationZ),
                translateAlongCameraAxis);
        }
#endif
    }
}
