using Ecs;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace console
{
    class Program
    {
        static void Main(string[] args)
        {
            SystemProcessor sp = new SystemProcessor();
            EntityManager em = new EntityManager(sp);
            MoverSystem ms = new MoverSystem();
            sp.RegisterSystem(ms);
            sp.RegisterSystem(new ControllerSystem());
            sp.RegisterSystem(new ConsoleRenderSystem());
            int entity = em.CreateEntity();
            em.AddComponent<Position>(entity);
            em.AddComponent(entity, new Velocity());

            NativeWindow nw = new NativeWindow(960, 540, "WINDOW", GameWindowFlags.Default, GraphicsMode.Default, DisplayDevice.Default);
            nw.KeyDown += OnKeyDown;
            nw.KeyUp += OnKeyUp;

            nw.Visible = true;

            while (nw.Exists)
            {
                nw.ProcessEvents();
                KeyboardState keyboardState = Keyboard.GetState();
                foreach (object key in Enum.GetValues(typeof(Key)))
                {
                    if (keyboardState.IsKeyDown((Key)key))
                    {
                        Console.WriteLine(key);
                    }
                }
                sp.Process(1f / 60f);
            }
        }

        private static void OnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            KeyboardManager.Up(e.Key);
        }

        private static void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            KeyboardManager.Down(e.Key);
        }
    }

    public struct Position
    {
        public Vector3 Value;
    }

    public struct Velocity
    {
        public Vector3 Value;
    }

    public class MoverSystem : GameSystem<Position, Velocity>
    {
        public override int GetTypeMask() => 3;
        public override void ProcessEntity(ref Position pos, ref Velocity vel)
        {
            pos.Value += vel.Value * Time.DeltaTime;
        }
    }

    public static class KeyboardManager
    {
        private static readonly HashSet<Key> _keys = new HashSet<Key>();

        public static void Down(Key key) => _keys.Add(key);
        public static void Up(Key key) => _keys.Remove(key);

        public static bool IsKeyDown(Key key) => _keys.Contains(key);
    }

    public class ControllerSystem : GameSystem
    {
        public override int GetTypeMask()
        {
            return 2;
        }

        public override void ProcessEntity(float deltaTime, int entity)
        {
            ref Velocity vel = ref Storages.GetStorage<Velocity>().GetComponent<Velocity>(entity);
            vel.Value = Vector3.Zero;

            if (KeyboardManager.IsKeyDown(Key.A))
            {
                vel.Value.X = -100f;
            }
            if (KeyboardManager.IsKeyDown(Key.D))
            {
                vel.Value.X = 100f;
            }
        }
    }

    public class ConsoleRenderSystem : GameSystem
    {
        public override int GetTypeMask()
        {
            return 1;
        }

        public override void ProcessEntity(float deltaTime, int entity)
        {
            ref Position pos = ref Storages.GetStorage<Position>().GetComponent<Position>(entity);
            Console.WriteLine("Position: " + pos.Value);
        }
    }

    public static class Time
    {
        public static float DeltaTime = 1f / 60f;
    }
}
