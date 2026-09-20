using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dynamic.WorldInterface.Data
{
    using System;
    using Hung.Utilities;
    using Gameplay.Character.WorldInterface;

    public class TilePosition
    {
        Vector2Int position;
        Vector2Int isHorizontal = Vector2Int.zero;
        Vector2Int isVertical = Vector2Int.zero;
        Vector2 globalPosition = Vector2.zero;
        public TilePosition(RaycastHit2D hit)
        {
            this.position = CalculateIntPosition(hit.point);
            if (hit.normal.x == 0)
            {
                isHorizontal = new Vector2Int(0, (int)hit.normal.y);
            }
            else
            {
                isVertical = new Vector2Int((int)hit.normal.x, 0);
            }

        }
        public TilePosition(Vector2 position)
        {
            this.position = CalculateIntPosition(position);

        }
        public TilePosition(Vector2Int position)
        {
            this.position = position;
        }
        public static Vector2Int CalculateIntPosition(Vector2 pos)
        {
            int x = Mathf.FloorToInt(Mathf.RoundToInt(pos.x * 100) / (16f * 4));
            int y = Mathf.FloorToInt(Mathf.RoundToInt(pos.y * 100) / (16f * 4));
            return new Vector2Int(x, y);
        }

        public static Vector2 CalculateFloatPosition(Vector2Int pos)
        {
            return new Vector2(pos.x * 0.16f * 4, pos.y * 0.16f * 4);
        }

        public void Copy(TilePosition tile)
        {
            this.position = tile.position;
            this.isHorizontal = tile.isHorizontal;
            this.isVertical = tile.isVertical;
            this.globalPosition = tile.GlobalPosition;
        }


        public Vector2Int Position
        {
            get => position;
            set
            {
                if (isHorizontal == Vector2Int.zero && isVertical == Vector2Int.zero)
                {
                    position = value;
                }
            }
        }
        public Vector2 GlobalPosition
        {
            get
            {
                if (globalPosition == default && isHorizontal == Vector2Int.zero && isVertical == Vector2Int.zero)
                    return CalculateFloatPosition(Position);
                else
                    return globalPosition;
            }

            set => globalPosition = value;
        }
        public Vector2Int IsHorizontal { get => isHorizontal; set => isHorizontal = value; }
        public Vector2Int IsVertical { get => isVertical; set => isVertical = value; }
    }
    public class Type1SensorData : SensorData
    {
        Dictionary<Vector2Int, TilePosition> Data = new Dictionary<Vector2Int, TilePosition>(100);
        private Vector2Int playerPosition;
        public Vector2Int PlayerPosition
        {
            get => playerPosition;
            set 
            {
                playerPosition = value;
            }
        }

        public bool Add(RaycastHit2D data)
        {
            Vector2Int key = TilePosition.CalculateIntPosition(data.point);
            if(key == new Vector2Int(103, -4))
            {
                Debug.Log("OK");
            }

            if (Data.ContainsKey(key))
            {
                if(Data[key].IsHorizontal == Vector2Int.zero && (int)data.normal.y != 0)
                {
                    Data[key].IsHorizontal = new Vector2Int(0, (int)data.normal.y);
                    return true;
                }

                if(Data[key].IsVertical == Vector2Int.zero && (int)data.normal.x != 0)
                {
                    Data[key].IsVertical = new Vector2Int((int)data.normal.x ,0);
                    return true;
                }
                return false;
            }
            else
            {
                TilePosition tile = new TilePosition(data);
                Data.Add(tile.Position, tile);
                return true;
            }
        }

        Vector2Int index;
        List<TilePosition> res = new List<TilePosition>();

        //Can be upgrade more
        public TilePosition GetPosition(Vector2Int pos)
        {
            if (Data.ContainsKey(pos))
            {
                return Data[pos];
            }
            else
            {
                return null;
            }
        }

        public List<TilePosition> GetPosition(Vector2Int mainPos,int radious, Vector2Int notHorizontal = default, Vector2Int notVertical = default)
        {
            index = mainPos;
            res.Clear();
            notHorizontal = MathHelper.Sign(notHorizontal);
            notVertical = MathHelper.Sign(notVertical);

            for (int i = 1; i <= radious; i++)
            {
                for (int x = mainPos.x - i; x <= mainPos.x + i; x++)
                {
                    index.Set(x, mainPos.y + i);
                    if (Data.ContainsKey(index))
                    {
                        res.Add(Data[index]);
                    }

                    index.Set(x, mainPos.y - i);
                    if (Data.ContainsKey(index))
                    {
                        res.Add(Data[index]);
                    }
                }

                for (int y = mainPos.y - i + 1; y <= mainPos.y + i - 1; y++)
                {
                    index.Set(mainPos.x - i,y);
                    if (Data.ContainsKey(index))
                    {
                        res.Add(Data[index]);
                    }

                    
                    index.Set(mainPos.x + i, y);
                    if (Data.ContainsKey(index))
                    {
                        res.Add(Data[index]);
                    }
                }                                              
            }

            
            //Can Improve(Not now)
            if (notHorizontal != default)            
                res.RemoveAll(t => t.IsHorizontal == notHorizontal);        
            if (notVertical != default)      
                res.RemoveAll(t => t.IsVertical == notVertical);
            
            return res;
        }

        public TilePosition GetPosition(Vector2Int mainPos,Vector2Int direction, int radius)
        {
            TilePosition res = null;
            direction = MathHelper.Sign(direction);
            Vector2Int index;
            for(int i = 1; i <= radius; i++)
            {
                index = mainPos + direction * i;
                if(Data.ContainsKey(index))
                {
                    res = Data[index];
                    return res;
                }
            }
            return res;
        }

        private Vector2Int mainDestination = new Vector2Int(100, -5);
        private TilePosition dynamicPosition = new TilePosition(Vector2Int.zero); // Need to update this(This is not update currently)
        private Vector2Int directionValue;
        public Vector2Int MainDestination
        {
            get => mainDestination;
            set
            {
                mainDestination = value;
                directionValue = mainDestination - dynamicPosition.Position;
            }
        }
        public TilePosition DynamicPosition
        {
            get => dynamicPosition;
            set
            {
                if (dynamicPosition.Position == value.Position) return;

                dynamicPosition.Copy(value);
                Debug.Log("Dynamic Pos:" + value.Position);
                directionValue = mainDestination - dynamicPosition.Position;
            }
        }
        public Vector2Int DirectionValue { get => directionValue; }
    }
}