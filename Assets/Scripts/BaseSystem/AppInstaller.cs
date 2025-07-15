using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AppInstaller : MonoInstaller
{
	[SerializeField]
	private BehaviorType _selectedBehaviorType = BehaviorType.MotionClient;

    public override void InstallBindings()
    {
        Container.Bind<IBehavior>()
                 .FromMethod(context => {
                     switch (_selectedBehaviorType) 
                     {
                         case BehaviorType.MotionClient:
                             return context.Container.Instantiate<MotionClientBehavior>();
                         case BehaviorType.VirtualCameraClient:
                             return context.Container.Instantiate<VirtualCameraClientBehavior>();
                         case BehaviorType.PropClient:
                             return context.Container.Instantiate<PropClientBehavior>();
                         default:
                             return context.Container.Instantiate<MotionClientBehavior>();
                     }
                 })
                 .AsSingle();
    }
}

public interface IBehavior
{
   void OnStart();
    void OnQuit();
}

public enum BehaviorType
{
    MotionClient,
    PropClient,
    VirtualCameraClient
}