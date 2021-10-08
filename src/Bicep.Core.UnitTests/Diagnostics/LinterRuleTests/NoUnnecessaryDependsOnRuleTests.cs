// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Analyzers.Linter.Rules;
using Bicep.Core.UnitTests.Assertions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Bicep.Core.UnitTests.Diagnostics.LinterRuleTests
{
    [TestClass]
    public class NoUnnecessaryDependsOnRuleTests : LinterRuleTestsBase
    {
        private void CompileAndTest(string text, OnCompileErrors onCompileErrors, string[] expectedMessages)
        {
            AssertLinterRuleDiagnostics(NoUnnecessaryDependsOnRule.Code, text, expectedMessages, onCompileErrors);
        }

        // This is the passing example in the docs
        [DataRow(@"
          resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
            name: 'name'
            location: resourceGroup().location
            sku: {
              name: 'F1'
              capacity: 1
            }
          }

          resource webApplication 'Microsoft.Web/sites@2018-11-01' = {
            name: 'name'
            location: resourceGroup().location
            properties: {
              serverFarmId: appServicePlan.id
            }
          }

          resource webApplication2 'Microsoft.Web/sites@2018-11-01' = {
            name: 'name2'
            location: resourceGroup().location
            properties: {
              serverFarmId: appServicePlan.id
            }
            dependsOn: []
          }

          resource webApplication3 'Microsoft.Web/sites@2018-11-01' = {
            name: 'name3'
            location: resourceGroup().location
            properties: {
              serverFarmId: appServicePlan.id
            }
            dependsOn: [
                webApplication
                webApplication
                webApplication2
            ]
          }
        ",
          OnCompileErrors.Fail
        )]
        [DataTestMethod]
        public void If_No_Simple_UnnecessaryDependsOn_ShouldPass(string text, OnCompileErrors onCompileErrors, params string[] expectedMessages)
        {
            CompileAndTest(text, onCompileErrors, expectedMessages);
        }

        // This is the failing example in the docs
        [DataRow(@"
            resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
              name: 'name'
              location: resourceGroup().location
              sku: {
                name: 'F1'
                capacity: 1
              }
            }

            resource webApplication 'Microsoft.Web/sites@2018-11-01' = {
              name: 'name'
              location: resourceGroup().location
              properties: {
                serverFarmId: appServicePlan.id
              }
              dependsOn: [
                appServicePlan
              ]
            }
        ",
          OnCompileErrors.Fail,
          "Remove unnecessary dependsOn entry 'appServicePlan'."
        )]
        [DataTestMethod]
        public void If_SimpleUnnecessaryDependsOn_ShouldFail(string text, OnCompileErrors onCompileErrors, params string[] expectedMessages)
        {
            CompileAndTest(text, onCompileErrors, expectedMessages);
        }

        [DataRow(@"
            resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
              name: 'name'
              location: resourceGroup().location
              sku: {
                name: 'F1'
                capacity: 1
              }
            }

            resource webApplication 'Microsoft.Web/sites@2018-11-01' = {
              name: 'name'
              location: resourceGroup().location
              properties: {
                serverFarmId: appServicePlan.id
              }
              dependsOn: [
                appServicePlan
              ]
            }
        ",
          OnCompileErrors.Fail,
          "Remove unnecessary dependsOn entry 'appServicePlan'."
        )]
        [DataTestMethod]
        public void If_Indirect_UnnecessaryDependsOn_ShouldFail(string text, OnCompileErrors onCompileErrors, params string[] expectedMessages)
        {
            CompileAndTest(text, onCompileErrors, expectedMessages);
        }

        [DataRow(@"
            resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
              name: 'name'
              location: resourceGroup().location
              sku: {
                name: 'F1'
                capacity: 1
              }
            }

            resource webApplication 'Microsoft.Web/sites@2018-11-01' = {
              name: 'name'
              location: resourceGroup().location
              properties: {
                serverFarmId: appServicePlan.id
              }
              dependsOn: [
                appServicePlan
              ]
            }
        ",
          OnCompileErrors.Fail,
          "Remove unnecessary dependsOn entry 'appServicePlan'."
        )]
        [DataTestMethod]
        public void If_Explicit_DependsOn_ToAncestor_ShouldFail(string text, OnCompileErrors onCompileErrors, params string[] expectedMessages)
        {
            CompileAndTest(text, onCompileErrors, expectedMessages);
        }

        [DataRow(@"
          param storageAccounts array

          resource storageAccountResources 'Microsoft.Storage/storageAccounts@2019-06-01' = [for storageName in storageAccounts: {
            name: storageName
            location: resourceGroup().location
            properties: {
              supportsHttpsTrafficOnly: true
            }
            kind: 'StorageV2'
            sku: {
              name: 'Standard_LRS'
            }
          }]

          resource dScript 'Microsoft.Resources/deploymentScripts@2019-10-01-preview' = {
            name: 'scriptWithStorage'
            location: resourceGroup().location
            kind: 'AzureCLI'
            identity: {
            }
            properties: {
                azCliVersion: '2.0.80'
                storageAccountSettings: {
                storageAccountName: storageAccountResources[0].name
                }
                retentionInterval: 'P1D'
            }
            dependsOn: [
                storageAccountResources[0]
            ]
          }
        ",
          OnCompileErrors.Fail,
          "Remove unnecessary dependsOn entry 'storageAccountResources[0]'."
        )]
        [DataTestMethod]
        public void If_ReferencesResourceByIndex_Should_WhatAsdff(string text, OnCompileErrors onCompileErrors, params string[] expectedMessages)
        {
            CompileAndTest(text, onCompileErrors, expectedMessages);
        }
    }
}

// TODO asdff: nested resources, syntax errors, duplicate dependson?, specify via id e.g. Test_Issue3182, dependson in existing resource?
// TODO asdff: dependson in modules, indexed dependencies from loops, cycles
// TODO asdff: child resources are automatically inferred
// todo asdff: multiple dependendsOn entries, multiple resources, multiple references to resource
// todo asdff: ignore stuff like stg.name inside dependsOn (I assume this would be an error)

// asdff ResourceDependsOnPropertyName