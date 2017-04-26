﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FakeService
{
    internal static class MockServiceRepository
    {
        private static readonly IDictionary<string, FakeService> MockServices =
            new Dictionary<string, FakeService>();

        internal static FakeService GetServiceMockById(string mockServiceId)
        {
            return MockServices.ContainsKey(mockServiceId) ? MockServices[mockServiceId] : null;
        }

        internal static void Register(FakeService mockService)
        {
            lock (MockServices)
            {
                if (MockServices.ContainsKey(mockService.ServiceId))
                {
                    throw new InvalidOperationException("ServiceId in use");
                }

                MockServices[mockService.ServiceId] = mockService;
            }
        }

        internal static void Unregister(FakeService mockService)
        {
            lock (MockServices)
            {
                if (!MockServices.ContainsKey(mockService.ServiceId))
                    throw new InvalidOperationException("MokService not registered");

                MockServices.Remove(mockService.ServiceId);
            }
        }
    }
}