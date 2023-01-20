using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DispenseChestTeleporter
{
    [Serializable]
    public class SerializeableVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public static SerializeableVector3 FromVector3(Vector3 vector)
        {
            return new SerializeableVector3() { X = vector.x, Y = vector.y, Z = vector.z };
        }

        public static implicit operator SerializeableVector3(Vector3 vector) => FromVector3(vector);
        public static implicit operator Vector3(SerializeableVector3 vector) => vector.ToVector3();

        public bool Equals(Vector3 vector)
        {
            return X == vector.x && Y == vector.y && Z == vector.z;
        }
    }
}
