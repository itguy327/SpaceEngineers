﻿#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Components;
using VRage.FileSystem;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;

#endregion

namespace Sandbox.Game.Entities.Character
{
    public abstract class MyCharacterDetectorComponent : MyCharacterComponent
    {
        IMyUseObject m_interactiveObject;

        protected MyHudNotification m_useObjectNotification;
        protected MyHudNotification m_showTerminalNotification;
        protected MyHudNotification m_openInventoryNotification;

        protected bool m_usingContinuously = false;

        public override void UpdateAfterSimulation10()
        {
            if (m_useObjectNotification != null && !m_usingContinuously)
                MyHud.Notifications.Add(m_useObjectNotification);

            m_usingContinuously = false;

            if (MySession.ControlledEntity == Character && !Character.IsSitting && !Character.IsDead)
            {
                DoDetection(MySession.GetCameraControllerEnum() != MyCameraControllerEnum.ThirdPersonSpectator);
            }
            else
            {
                if (MySession.ControlledEntity == Character)
                {
                    MyHud.SelectedObjectHighlight.Visible = false;
                }
            }
        }

        protected abstract void DoDetection(bool useHead);

        public IMyUseObject UseObject
        {
            get { return m_interactiveObject; }
            set
            {
                bool changed = value != m_interactiveObject;

                if (changed)
                {
                    if (m_interactiveObject != null)
                    {
                        UseClose();
                        InteractiveObjectRemoved();
                    }

                    m_interactiveObject = value;
                    InteractiveObjectChanged();
                }
            }
        }

        void UseClose()
        {
            if (UseObject != null && UseObject.IsActionSupported(UseActionEnum.Close))
            {
                UseObject.Use(UseActionEnum.Close, Character);
            }
        }

        void InteractiveObjectRemoved()
        {
            Character.RemoveNotification(ref m_useObjectNotification);
            Character.RemoveNotification(ref m_showTerminalNotification);
            Character.RemoveNotification(ref m_openInventoryNotification);
        }

        void InteractiveObjectChanged()
        {
            if (MySession.ControlledEntity == this.Character && UseObject != null)
            {
                GetNotification(UseObject, UseActionEnum.Manipulate, ref m_useObjectNotification);
                GetNotification(UseObject, UseActionEnum.OpenTerminal, ref m_showTerminalNotification);
                GetNotification(UseObject, UseActionEnum.OpenInventory, ref m_openInventoryNotification);
                var useText = m_useObjectNotification != null ? m_useObjectNotification.Text : MySpaceTexts.Blank;
                var showText = m_showTerminalNotification != null ? m_showTerminalNotification.Text : MySpaceTexts.Blank;
                var openText = m_openInventoryNotification != null ? m_openInventoryNotification.Text : MySpaceTexts.Blank;
                if (useText != MySpaceTexts.Blank)
                    MyHud.Notifications.Add(m_useObjectNotification);
                if (showText != MySpaceTexts.Blank && showText != useText)
                    MyHud.Notifications.Add(m_showTerminalNotification);
                if (openText != MySpaceTexts.Blank && openText != showText && openText != useText)
                    MyHud.Notifications.Add(m_openInventoryNotification);
            }
        }

        void GetNotification(IMyUseObject useObject, UseActionEnum actionType, ref MyHudNotification notification)
        {
            if ((useObject.SupportedActions & actionType) != 0)
            {
                var actionInfo = useObject.GetActionInfo(actionType);
                Character.RemoveNotification(ref notification);
                notification = new MyHudNotification(actionInfo.Text, 0, level: actionInfo.IsTextControlHint ? MyNotificationLevel.Control : MyNotificationLevel.Normal);
                if (!MyInput.Static.IsJoystickConnected())
                {
                    notification.SetTextFormatArguments(actionInfo.FormatParams);
                }
                else
                {
                    if (actionInfo.JoystickText.HasValue)
                        notification.Text = actionInfo.JoystickText.Value;
                    if (actionInfo.JoystickFormatParams != null)
                        notification.SetTextFormatArguments(actionInfo.JoystickFormatParams);
                }
            }
        }

        public void UseContinues()
        {
            MyHud.Notifications.Remove(m_useObjectNotification);
            m_usingContinuously = true;
        }

    }
}
