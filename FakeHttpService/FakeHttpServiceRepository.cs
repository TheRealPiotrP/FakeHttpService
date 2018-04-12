// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace FakeHttpService
{
    public static class FakeHttpServiceRepository
    {
        private static readonly ConcurrentDictionary<string, FakeHttpService> MockServices =
            new ConcurrentDictionary<string, FakeHttpService>();

        public static FakeHttpService GetServiceMockById(string mockServiceId)
        {
            MockServices.TryGetValue(mockServiceId, out var service);
            return service;
        }

        public static void Register(FakeHttpService mockHttpService)
        {
            if (!MockServices.TryAdd(mockHttpService.ServiceId, mockHttpService))
            {
                throw new InvalidOperationException("ServiceId in use");
            }
        }

        public static void Unregister(FakeHttpService mockHttpService)
        {
            if (!MockServices.TryRemove(mockHttpService.ServiceId, out var _))
            {
                throw new InvalidOperationException("MockService not registered");
            }
        }
    }
}