using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP
{
    internal static class MapBuilder
    {
        public static void BuildGrassGrid(
            System.Collections.Generic.List<Prop> props,
            Model grassModel,
            Effect grassEffect,
            Texture2D? grassTexture,
            int gridSize,
            float grassSize)
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    props.Add(new Prop(grassModel, grassEffect, new Vector3(x * grassSize, 0, z * grassSize)));
                }
            }
        }

        public static void BuildFences(
            System.Collections.Generic.List<Prop> props,
            Model fenceModel,
            Effect shader,
            int gridSize,
            float grassSize)
        {
            float half = grassSize / 2f;
            float max = (gridSize - 1) * grassSize;

            var rot90 = new Vector3(0, MathHelper.ToRadians(90), 0);

            // Top and bottom rows
            for (int x = 0; x < gridSize; x++)
            {
                var posTop = new Vector3(x * grassSize, 0, -half);
                var posBottom = new Vector3(x * grassSize, 0, max + half);

                props.Add(new Prop(fenceModel, shader, posTop));
                props.Add(new Prop(fenceModel, shader, posBottom));
            }

            // Left and right columns
            for (int z = 0; z < gridSize; z++)
            {
                var posLeft = new Vector3(-half, 0, z * grassSize);
                var posRight = new Vector3(max + half, 0, z * grassSize);

                props.Add(new Prop(fenceModel, shader, posLeft, rotation: rot90));
                props.Add(new Prop(fenceModel, shader, posRight, rotation: rot90));
            }
        }

        public static void PlaceRandomTrees(
            System.Collections.Generic.List<Prop> props,
            Random random,
            Model smallTree,
            Model largeTree,
            int treeCount,
            float worldMax,
            float edgeBandMin,
            float edgeBandMax,
            float edgeBandHighMin,
            float edgeBandHighMax,
            Effect smallTreeEffect,
            Effect largeTreeEffect)
        {
            for (int i = 0; i < treeCount; i++)
            {
                var useLarge = random.Next(0, 2) == 0;
                var model = useLarge ? largeTree : smallTree;
                var effect = useLarge ? largeTreeEffect : smallTreeEffect;

                bool xIsConstrained = random.Next(0, 2) == 0;

                float posX, posZ;

                if (xIsConstrained)
                {
                    bool xUseHighEdge = random.Next(0, 2) == 0;
                    posX = xUseHighEdge
                        ? edgeBandHighMin + random.NextSingle() * (edgeBandHighMax - edgeBandHighMin)
                        : edgeBandMin + random.NextSingle() * (edgeBandMax - edgeBandMin);

                    posZ = random.NextSingle() * worldMax;
                }
                else
                {
                    bool zUseHighEdge = random.Next(0, 2) == 0;
                    posZ = zUseHighEdge
                        ? edgeBandHighMin + random.NextSingle() * (edgeBandHighMax - edgeBandHighMin)
                        : edgeBandMin + random.NextSingle() * (edgeBandMax - edgeBandMin);

                    posX = random.NextSingle() * worldMax;
                }

                props.Add(new Prop(model, effect, new Vector3(posX, 0, posZ)));
            }
        }
    }
}
