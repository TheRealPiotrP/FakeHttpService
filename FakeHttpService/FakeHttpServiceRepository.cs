// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FakeHttpService
{
    public static class FakeHttpServiceRepository
    {
        private static readonly IDictionary<string, FakeHttpService> MockServices =
            new Dictionary<string, FakeHttpService>();

        public static FakeHttpService GetServiceMockById(string mockServiceId)
        {
            return MockServices.ContainsKey(mockServiceId) ? MockServices[mockServiceId] : null;
        }

        public static void Register(FakeHttpService mockHttpService)
        {
            lock (MockServices)
            {
                if (MockServices.ContainsKey(mockHttpService.ServiceId))
                {
                    throw new InvalidOperationException("ServiceId in use");
                }

                MockServices[mockHttpService.ServiceId] = mockHttpService;
            }
        }

        public static void Unregister(FakeHttpService mockHttpService)
        {
            lock (MockServices)
            {
                if (!MockServices.ContainsKey(mockHttpService.ServiceId))
                    throw new InvalidOperationException("MokService not registered");

                MockServices.Remove(mockHttpService.ServiceId);
            }
        }
    }
}