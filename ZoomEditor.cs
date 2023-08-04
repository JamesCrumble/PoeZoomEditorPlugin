using ExileCore;
using System;
using ProcessMemoryUtilities.Managed;
using ExileCore.Shared;
using System.Collections;

namespace ZoomEditor
{
    public class ZoomEditor : BaseSettingsPlugin<ZoomEditorSettings>
    {

        private IntPtr PoeRWProcessHandle = IntPtr.Zero;  // The only reason this is exists couse PoeHelper etc. has read only flaged handle...
        private const float DefaultStaticZoomValue = (float)-1.0;

        private const long DynamicZoomOffset = 0x3d8;
        private const long StaticZoomOffset = DynamicZoomOffset + 0x4;

        private long DynamicZoomAddress;
        private long StaticZoomAddress;
        private float CurrentStaticZoomValue = DefaultStaticZoomValue;

        public float ReadFloat(long address)
        {
            try
            {
                byte[] buffer = new byte[4];
                NativeWrapper.ReadProcessMemoryArray(PoeRWProcessHandle, new IntPtr(address), buffer, 0, 4);
                return BitConverter.ToSingle(buffer, 0);
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"Cannot read memory at \"{address}\" address => {e}");
                throw;
            }
        }

        public void WriteFloat(long address, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            bool statusCode = NativeWrapper.WriteProcessMemoryArray(PoeRWProcessHandle, new IntPtr(address), bytes);
            if (!statusCode)
            {
                DebugWindow.LogError($"Cannot write \"{value}\" into zoom \"{address}\" address");
            }
        }

        private void InitializePoeRWProcessHadle()
        {
            if (PoeRWProcessHandle != IntPtr.Zero)
            {
                return;
            }

            PoeRWProcessHandle = NativeWrapper.OpenProcess(
                ProcessMemoryUtilities.Native.ProcessAccessFlags.ReadWrite, GameController.Memory.Process.Id
            );

            if (PoeRWProcessHandle == IntPtr.Zero)
            {
                DebugWindow.LogError("Cannot initialise PoeRWProcessHandle for unkown reasons");
            }
        }

        private void ReloadAction()
        {
            Settings.ZoomValue.SetValueNoEvent(DefaultStaticZoomValue.ToString("0.0"));
            WriteFloat(StaticZoomAddress, DefaultStaticZoomValue);
        }

        public override void OnLoad()
        {
            Settings.Reload.OnPressed += ReloadAction;
        }

        public override bool Initialise()
        {
            try
            {
                InitializePoeRWProcessHadle();
                if (PoeRWProcessHandle == IntPtr.Zero)
                {
                    return false;
                }

                GameController.LeftPanel.WantUse(() => Settings.Enable);
                DynamicZoomAddress = GameController.IngameState.Camera.Address + DynamicZoomOffset;
                StaticZoomAddress = GameController.IngameState.Camera.Address + StaticZoomOffset;

                var zoomUpdaterCoroutine = new Coroutine(ZoomUpdaterEvent(), this);
                Core.ParallelRunner.Run(zoomUpdaterCoroutine);
            }
            catch (Exception exc)
            {
                DebugWindow.LogError($"{nameof(ZoomEditor)} -> {exc}");
                return false;
            }

            return true;
        }

        public new void LogMsg(string msg)
        {
            if (!Settings.DebugOutput.Value)
            {
                return;
            }
            DebugWindow.LogMsg(msg);
        }

        private IEnumerator ZoomUpdaterEvent()
        {
            while (true)
            {
                InitializePoeRWProcessHadle();
                try
                {
                    float newStaticZoomValue = float.Parse(Settings.ZoomValue.Value);

                    if (CurrentStaticZoomValue != newStaticZoomValue)
                    {
                        WriteFloat(StaticZoomAddress, newStaticZoomValue);
                        CurrentStaticZoomValue = newStaticZoomValue;
                    }

                    LogMsg($"Current values => DynamicZoomValue=\"{ReadFloat(DynamicZoomAddress)}\", StaticZoomValue=\"{ReadFloat(StaticZoomAddress)}\"");
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"{nameof(ZoomEditor)} can't update zoom value {e}");
                }


                yield return new WaitTime(500);
            }
        }
    }
}
