using System;
using System.Collections.Generic;
using Ecs;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var cm = new ComponentManager();

            var sp = new SystemProcessor(cm);
            sp.RegisterSystem(new MoverSystem());
            sp.RegisterSystem(new ControllerSystem());
            sp.RegisterSystem(new ConsoleRenderSystem());

            var em = new EntityManager(sp, cm);
            var entity = em.CreateEntity();
            em.AddComponent<Position>(entity);
            em.AddComponent(entity, new Velocity());

            var nw = new NativeWindow(960, 540, "WINDOW", GameWindowFlags.Default, GraphicsMode.Default,
                DisplayDevice.Default);
            nw.KeyDown += OnKeyDown;
            nw.KeyUp += OnKeyUp;

            nw.Visible = true;

            while (nw.Exists)
            {
                nw.ProcessEvents();
                var keyboardState = Keyboard.GetState();
                foreach (var key in Enum.GetValues(typeof(Key)))
                {
                    if (keyboardState.IsKeyDown((Key) key))
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
        public override void ProcessEntity(ref Position pos, ref Velocity vel)
        {
            pos.Value += vel.Value * Time.DeltaTime;
        }
    }

    public static class KeyboardManager
    {
        private static readonly HashSet<Key> Keys = new HashSet<Key>();

        public static void Down(Key key)
        {
            Keys.Add(key);
        }

        public static void Up(Key key)
        {
            Keys.Remove(key);
        }

        public static bool IsKeyDown(Key key)
        {
            return Keys.Contains(key);
        }
    }

    public class ControllerSystem : GameSystem<Velocity>
    {
        public override void ProcessEntity(ref Velocity vel)
        {
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

    public class ConsoleRenderSystem : GameSystem<Position>
    {
        public override void ProcessEntity(ref Position pos)
        {
            Console.WriteLine($"Position: {pos.Value}");
        }
    }

    public static class Time
    {
        public static float DeltaTime = 1f / 60f;
    }
}