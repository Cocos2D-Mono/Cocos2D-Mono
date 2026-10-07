using System;
using System.Reflection;
using Microsoft.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Cocos2D;

public class CCAccelerometer
{
#if (ANDROID || IOS) && !TVOS
    // the accelerometer sensor on the device
    private static MonoGame.Framework.Devices.Sensors.Accelerometer accelerometer = null;
#endif

    private const float TG3_GRAVITY_EARTH = 9.80665f;
    private ICCAccelerometerDelegate _accelDelegate;
    private readonly CCAcceleration _accelerationValue = new CCAcceleration();

    private bool _active;
    private bool _emulation;

    static CCAccelerometer()
    {
#if (ANDROID || IOS) && !TVOS
        try
        {
            if (MonoGame.Framework.Devices.Sensors.Accelerometer.IsSupported)
            {
                accelerometer = new MonoGame.Framework.Devices.Sensors.Accelerometer();
            }
            else
            {
                CCLog.Log("Accelerometer not supported on this device. CCAccelerometer will default to emulation code.");
            }
        }
        catch (Exception ex)
        {
            accelerometer = null;
            CCLog.Log(ex.ToString());
            CCLog.Log("No accelerometer on platform. CCAccelerometer will default to emulation code.");
        }
#endif
    }

    private void ResetAccelerometer()
    {
        _accelerationValue.X = 0;
        _accelerationValue.Y = 0;
        _accelerationValue.Z = 0;
    }

    public void SetDelegate(ICCAccelerometerDelegate pDelegate)
    {
        _accelDelegate = pDelegate;

        if (pDelegate != null && !_active)
        {
#if (ANDROID || IOS) && !TVOS
                try
            {
                if (accelerometer != null && MonoGame.Framework.Devices.Sensors.Accelerometer.IsSupported)
                {
                    accelerometer.CurrentValueChanged += accelerometer_CurrentValueChanged;
                    accelerometer.Start();
                    _active = true;
                }
                else
                {
                    _active = false;
                }
            }
            catch (Exception)
            {
                accelerometer = null;
                _active = false;
            }
#endif
            if (!_active)
            {
                _active = true;
                _emulation = true;
            }
            else
            {
                _emulation = false;
            }
        }
        else
        {
            if (_active && !_emulation)
            {
#if (ANDROID || IOS) && !TVOS
                if (accelerometer != null)
                {
                    try
                    {
                        accelerometer.CurrentValueChanged -= accelerometer_CurrentValueChanged;
                        accelerometer.Stop();
                    }
                    catch (Exception)
                    {
                        // Sensor can be in a bad / partial state on teardown; ignore.
                    }
                }
#endif
            }
            
            ResetAccelerometer();

            _active = false;
            _emulation = false;
        }
    }


#if (ANDROID || IOS) && !TVOS
    private void accelerometer_CurrentValueChanged(object sender, MonoGame.Framework.Devices.Sensors.SensorReadingEventArgs<MonoGame.Framework.Devices.Sensors.AccelerometerReading> e)
    {

        // We have to use reflection to get the Vector3 value out of Acceleration
        // What happens is that the Sensor used XNA Vector3 and what we have done is replaced
        // the XNA with MonoGame which our Sensor does not compile against.
        //
        // Result is this ugly hack.
        object val = e.SensorReading.GetType()
         .GetProperty("Acceleration",
                      BindingFlags.FlattenHierarchy |
                      BindingFlags.Instance |
                      BindingFlags.Public)
         .GetValue(e.SensorReading, null);

        if (val == null)
            return;

        // store the accelerometer value in our acceleration object to be updated.
        UpdateAccelerationValue(val.ToString());

        _accelerationValue.TimeStamp = e.SensorReading.Timestamp.Ticks;
    }

    private void UpdateAccelerationValue(string acceleration)
    {
        string[] temp = acceleration.Substring(1, acceleration.Length - 2).Split(':');
        // The format of the string is {X: 0000 Y: 0000 Z: 0000}
        // Here we need to parse differently so that we can get a constant value back
        //  Cocos2D-XNA mapps the Sensor reading of the X value to be our Y value
        //  and the Y value to our X value.  Also the values need to be negated so that 
        //  it maps correctly.
        _accelerationValue.Y = -float.Parse(temp[1].Substring(0, temp[1].Length - 1));
        _accelerationValue.X = -float.Parse(temp[2].Substring(0, temp[2].Length - 1));
        _accelerationValue.Z = float.Parse(temp[3]);

    }

    private static Vector3 ParseVector3(string acceleration)
    {

        string[] temp = acceleration.Substring(1, acceleration.Length - 2).Split(':');
        float x = float.Parse(temp[1].Substring(0, temp[1].Length - 1));
        float y = float.Parse(temp[2].Substring(0, temp[2].Length - 1));
        float z = float.Parse(temp[3]);
        Vector3 rValue = new Vector3(x, y, z);
        return rValue;
    }

#endif

    public void Update()
    {
        if (_accelDelegate != null)
        {
            if (_emulation)
            {
                // if we're in the emulator, we'll generate a fake acceleration value using the arrow keys
                // press the pause/break key to toggle keyboard input for the emulator
                KeyboardState keyboardState = Keyboard.GetState();

                var stateValue = new Vector3();

                stateValue.Z = -1;

                if (keyboardState.IsKeyDown(Keys.Left))
                    stateValue.X = -.1f;
                if (keyboardState.IsKeyDown(Keys.Right))
                    stateValue.X = .1f;
                if (keyboardState.IsKeyDown(Keys.Up))
                    stateValue.Y = -.1f;
                if (keyboardState.IsKeyDown(Keys.Down))
                    stateValue.Y = .1f;

                stateValue.Normalize();

                _accelerationValue.X = stateValue.X;
                _accelerationValue.Y = stateValue.Y;
                _accelerationValue.Z = stateValue.Z;
                _accelerationValue.TimeStamp = DateTime.Now.Ticks;
            }

            _accelDelegate.DidAccelerate(_accelerationValue);
        }
    }
}
