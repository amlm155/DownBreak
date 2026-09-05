using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mm_Budier
{
    public interface ICubeBehaviour
    {
        void OnPlaced(CubeInstance cubeInstance);
        void OnRemoved();
        void OnInteract(CubeInstance cubeInstance);
    }
}