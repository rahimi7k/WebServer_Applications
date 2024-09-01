using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Library {

	public class ThreadLock<T> {


		public readonly ConcurrentDictionary<T, Lazy<SemaphoreSlim>> list1 = new ConcurrentDictionary<T, Lazy<SemaphoreSlim>>();
		public readonly ConcurrentDictionary<T, Lazy<SemaphoreSlim>> list2 = new ConcurrentDictionary<T, Lazy<SemaphoreSlim>>();
		public readonly ConcurrentDictionary<T, Lazy<SemaphoreSlim>> list3 = new ConcurrentDictionary<T, Lazy<SemaphoreSlim>>();

		// private int time = StringUtils.ParseInt(DateTime.Now.ToString("m"));
		public int time = 10;
		private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public async Task<IDisposable> LockAsync(T key) {

			SemaphoreSlim semaphore = GetSemaphore(key).Value;
			await semaphore.WaitAsync();

			return new Disposable(new Action(delegate () {
				semaphore.Release();
			}));
		}


		public IDisposable Lock(T key) {

			SemaphoreSlim semaphore = GetSemaphore(key).Value;
			semaphore.Wait();

			return new Disposable(new Action(delegate () {
				semaphore.Release();
			}));
		}


		private Lazy<SemaphoreSlim> GetSemaphore(T key) {
			if (time == 10) {
				time = 30;
			} else if (time == 30) {
				time = 50;
			} else if (time == 50) {
				time = 10;
			}
			if (time != StringUtils.ParseInt(DateTime.Now.ToString("m"))) {
				//if (StringUtils.Random(0, 100) == 1) {
				CleanUp();
				//}
			}


			Lazy<SemaphoreSlim> lazy = null;

			if (0 <= time && time <= 20) {

				list1.TryGetValue(key, out lazy);
				if (lazy == null) {
					list3.TryGetValue(key, out lazy);
					if (lazy == null) {
						lazy = list1.GetOrAdd(key, new Lazy<SemaphoreSlim>(new Func<SemaphoreSlim>(delegate () {
							return new SemaphoreSlim(1, 1);
						}), LazyThreadSafetyMode.ExecutionAndPublication));
					} else {
						list1.TryAdd(key, lazy);
						list3.TryRemove(key, out _);
					}
				}



			} else if (21 <= time && time <= 40) {

				list2.TryGetValue(key, out lazy);
				if (lazy == null) {
					list1.TryRemove(key, out lazy);
					if (lazy == null) {
						lazy = list2.GetOrAdd(key, new Lazy<SemaphoreSlim>(new Func<SemaphoreSlim>(delegate () {
							return new SemaphoreSlim(1, 1);
						}), LazyThreadSafetyMode.ExecutionAndPublication));
					} else {
						list2.TryAdd(key, lazy);
						list1.TryRemove(key, out _);
					}
				}


			} else if (41 <= time && time <= 59) {

				list3.TryGetValue(key, out lazy);
				if (lazy == null) {
					list2.TryRemove(key, out lazy);
					if (lazy == null) {
						lazy = list3.GetOrAdd(key, new Lazy<SemaphoreSlim>(new Func<SemaphoreSlim>(delegate () {
							return new SemaphoreSlim(1, 1);
						}), LazyThreadSafetyMode.ExecutionAndPublication));
					} else {
						list3.TryAdd(key, lazy);
						list2.TryRemove(key, out _);
					}
				}


			}
			return lazy;
		}



		private void CleanUp() {

			semaphore.Wait();
			if (time == StringUtils.ParseInt(DateTime.Now.ToString("m"))) {
				semaphore.Release();
				return;
			}
			time = StringUtils.ParseInt(DateTime.Now.ToString("m"));
			semaphore.Release();

			if (0 <= time && time <= 20) {
				list2.Clear();

			} else if (21 <= time && time <= 40) {
				list3.Clear();

			} else if (41 <= time && time <= 59) {
				list1.Clear();
			}
		}

		private sealed class Disposable : IDisposable {

			private Action action;

			public Disposable(Action action) {
				this.action = action;
			}

			public void Dispose() {
				Action action = this.action;
				if (action != null) {
					action();
				}
			}
		}
	}
}

