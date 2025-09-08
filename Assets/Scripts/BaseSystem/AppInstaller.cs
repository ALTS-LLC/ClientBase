using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AppInstaller : MonoInstaller
{
	public BehaviorType SelectedBehaviorType = BehaviorType.MotionClient;

    [SerializeField]
    private List<DirectorBase> _directors = new List<DirectorBase>();

    public override void InstallBindings()
    {
        Container.Bind<IBehavior>()
                 .FromMethod(context => {
                     switch (SelectedBehaviorType) 
                     {
                         case BehaviorType.None:
                             return null;                                                 
                         case BehaviorType.MotionClient:
                             MotionClientDirector motionClientDirector = Instantiate(GetBehaviorDirector<MotionClientDirector>());
                             motionClientDirector.transform.parent = gameObject.transform;
                             return context.Container.Instantiate<MotionClientBehavior>();
                         default:
                             return null;
                     }
                 })
                 .AsSingle();
    }
    
    private T GetBehaviorDirector<T>()
    {
        T returnval = default;
        foreach (var director in _directors)
        {
            if (director.TryGetComponent<T>(out T directorType))
            {
                returnval = directorType;
            }
        }
        return returnval;
    }
}

public interface IBehavior
{
   void OnStart();
    void OnQuit();
}

public enum BehaviorType
{
    None,
    MotionClient,
}