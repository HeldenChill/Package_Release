using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Hung.UI.Tests
{
    public sealed class QueueProbeAnim : UIAnim
    {
        public readonly List<ANIM> Started = new List<ANIM>();

        public override IReadOnlyList<Propertys> Datas => Array.Empty<Propertys>();
        public override void Stop() { }

        public override void Play(ANIM anim)
        {
            if (!TryBeginOrQueue(anim)) return;
            state = anim;
            Started.Add(anim);
        }

        public void Complete(ANIM anim) => OnAnimExit((int)anim);
        public int Generation => CurrentGeneration;
        public void CompleteDelayed(ANIM anim, int generation) => CompleteIfCurrent((int)anim, generation);
    }

    public class UIAnimQueueTests
    {
        private GameObject gameObject;
        private QueueProbeAnim anim;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("QueueProbeAnim");
            anim = gameObject.AddComponent<QueueProbeAnim>();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(gameObject);

        [Test]
        public void Hide_requested_during_show_starts_after_show_once()
        {
            anim.Play(UIAnim.ANIM.SHOW);
            anim.Play(UIAnim.ANIM.HIDE);
            anim.Play(UIAnim.ANIM.HIDE);
            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW }, anim.Started);

            anim.Complete(UIAnim.ANIM.SHOW);
            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW, UIAnim.ANIM.HIDE }, anim.Started);
        }

        [Test]
        public void Interrupt_drops_queued_hide()
        {
            anim.Play(UIAnim.ANIM.SHOW);
            anim.Play(UIAnim.ANIM.HIDE);
            anim.Interrupt();
            anim.Complete(UIAnim.ANIM.SHOW);

            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW }, anim.Started);
        }

        [Test]
        public void Delayed_completion_from_interrupted_show_cannot_finish_reopened_show()
        {
            anim.Play(UIAnim.ANIM.SHOW);
            int oldGeneration = anim.Generation;
            anim.Interrupt();
            anim.Play(UIAnim.ANIM.SHOW);

            anim.CompleteDelayed(UIAnim.ANIM.SHOW, oldGeneration);
            anim.Play(UIAnim.ANIM.HIDE);
            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW, UIAnim.ANIM.SHOW }, anim.Started);
        }

        [Test]
        public void Queued_outro_callback_waits_for_hide_completion()
        {
            int completions = 0;
            anim.Play(UIAnim.ANIM.SHOW);
            anim.PlayOutro(() => completions++);

            anim.Complete(UIAnim.ANIM.SHOW);
            Assert.AreEqual(0, completions);
            anim.Complete(UIAnim.ANIM.HIDE);
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void Exit_listener_can_start_new_show_without_old_exit_clearing_it()
        {
            anim.Play(UIAnim.ANIM.SHOW);
            int firstGeneration = anim.Generation;
            anim._OnAnimExit += (_, code) =>
            {
                if (code != (int)UIAnim.ANIM.SHOW || anim.Generation != firstGeneration) return;
                anim.Play(UIAnim.ANIM.SHOW);
            };

            anim.Complete(UIAnim.ANIM.SHOW);
            anim.Play(UIAnim.ANIM.HIDE);

            CollectionAssert.AreEqual(new[] { UIAnim.ANIM.SHOW, UIAnim.ANIM.SHOW }, anim.Started);
        }
    }
}
