using Mm_Budier;
using UnityEngine;

public abstract class CubeBehaviour : MonoBehaviour, ICubeBehaviour
{
    protected CubeInstance curCubeInstance;
    public virtual void OnPlaced(CubeInstance cubeInstance){
        this.curCubeInstance = cubeInstance;
    }
    public virtual void OnRemoved(){
    }  
    public virtual void OnUpdated(CubeInstance cubeInstance){}
    public virtual void OnInteract(CubeInstance cubeInstance){}
}
