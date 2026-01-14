// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Memory;
using Xunit;

namespace Tutel.VirtualMachine.Tests;

/// <summary>
/// Tests for garbage collection in Tutel VM.
/// </summary>
public class GarbageCollectorTests
{
    [Fact]
    public void UnreachableArrayIsCollected()
    {
        // Arrange
        MemoryManager memory = new(0);
        ManagedHeap heap = new();

        // Act: Create array and lose reference
        long taggedHandle = heap.Allocate(10);
        int handle = Value.GetHandle(taggedHandle);

        // Verify array exists
        Assert.NotNull(heap.GetArray(handle));
        Assert.Equal(1, heap.ArrayCount);

        // Run GC - array should be collected
        heap.Collect(memory);

        // Assert: Array should be freed
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(handle));
        Assert.Equal(0, heap.ArrayCount);
    }

    [Fact]
    public void ArrayInGlobalVariableIsNotCollected()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create array and store in global variable
        long taggedHandle = heap.Allocate(5);
        int handle = Value.GetHandle(taggedHandle);
        memory.SetGlobal(0, taggedHandle);

        // Verify array exists
        Assert.Equal(1, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Array should still exist
        Assert.NotNull(heap.GetArray(handle));
        Assert.Equal(1, heap.ArrayCount);
        Assert.Equal(taggedHandle, memory.GetGlobal(0));
    }

    [Fact]
    public void ArrayInLocalVariableIsNotCollected()
    {
        // Arrange
        MemoryManager memory = new(0);
        ManagedHeap heap = new();

        // Act: Create frame and store array in local variable
        memory.CallStack.PushFrame(0, -1, 1);
        long taggedHandle = heap.Allocate(3);
        int handle = Value.GetHandle(taggedHandle);
        memory.SetLocal(0, taggedHandle);

        // Verify array exists
        Assert.Equal(1, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Array should still exist
        Assert.NotNull(heap.GetArray(handle));
        Assert.Equal(1, heap.ArrayCount);
        Assert.Equal(taggedHandle, memory.GetLocal(0));
    }

    [Fact]
    public void ArrayOnStackIsNotCollected()
    {
        // Arrange
        MemoryManager memory = new(0);
        ManagedHeap heap = new();

        // Act: Push array handle onto stack
        long taggedHandle = heap.Allocate(7);
        int handle = Value.GetHandle(taggedHandle);
        memory.OperandStack.Push(taggedHandle);

        // Verify array exists
        Assert.Equal(1, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Array should still exist
        Assert.NotNull(heap.GetArray(handle));
        Assert.Equal(1, heap.ArrayCount);
        Assert.Equal(taggedHandle, memory.OperandStack.Pop());
    }

    [Fact]
    public void NestedArraysArePreserved()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create parent array containing handle to child array
        long childHandle = heap.Allocate(3);
        long parentHandle = heap.Allocate(1);
        int parentIdx = Value.GetHandle(parentHandle);

        // Store child handle in parent array
        heap.SetElement(parentIdx, 0, childHandle);

        // Store parent in global
        memory.SetGlobal(0, parentHandle);

        // Verify both arrays exist
        Assert.Equal(2, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Both arrays should still exist
        Assert.Equal(2, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(Value.GetHandle(parentHandle)));
        Assert.NotNull(heap.GetArray(Value.GetHandle(childHandle)));
        Assert.Equal(childHandle, heap.GetElement(parentIdx, 0));
    }

    [Fact]
    public void CircularReferencesArePreserved()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create two arrays that reference each other
        long array1Handle = heap.Allocate(1);
        long array2Handle = heap.Allocate(1);
        int array1Idx = Value.GetHandle(array1Handle);
        int array2Idx = Value.GetHandle(array2Handle);

        // Create circular reference
        heap.SetElement(array1Idx, 0, array2Handle);
        heap.SetElement(array2Idx, 0, array1Handle);

        // Store one in global to make both reachable
        memory.SetGlobal(0, array1Handle);

        // Verify both arrays exist
        Assert.Equal(2, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Both arrays should still exist
        Assert.Equal(2, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(array1Idx));
        Assert.NotNull(heap.GetArray(array2Idx));
        Assert.Equal(array2Handle, heap.GetElement(array1Idx, 0));
        Assert.Equal(array1Handle, heap.GetElement(array2Idx, 0));
    }

    [Fact]
    public void MultipleArraysOnlyLiveOnesPreserved()
    {
        // Arrange
        MemoryManager memory = new(2);
        ManagedHeap heap = new();

        // Act: Create multiple arrays
        long live1 = heap.Allocate(1);
        long live2 = heap.Allocate(2);
        long dead1 = heap.Allocate(3);
        long dead2 = heap.Allocate(4);

        // Store live arrays in globals
        memory.SetGlobal(0, live1);
        memory.SetGlobal(1, live2);

        // Verify all arrays exist
        Assert.Equal(4, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Only live arrays should exist
        Assert.Equal(2, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(Value.GetHandle(live1)));
        Assert.NotNull(heap.GetArray(Value.GetHandle(live2)));
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(Value.GetHandle(dead1)));
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(Value.GetHandle(dead2)));
    }

    [Fact]
    public void ArrayInMultipleRootsIsPreserved()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create array and store in multiple roots
        long taggedHandle = heap.Allocate(5);
        int handle = Value.GetHandle(taggedHandle);

        memory.SetGlobal(0, taggedHandle);
        memory.OperandStack.Push(taggedHandle);
        memory.CallStack.PushFrame(0, -1, 1);
        memory.SetLocal(0, taggedHandle);

        // Verify array exists
        Assert.Equal(1, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Array should still exist
        Assert.Equal(1, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(handle));
    }

    [Fact]
    public void FreeSlotsAreReusedAfterGC()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create and free array
        long handle1 = heap.Allocate(10);
        int idx1 = Value.GetHandle(handle1);
        Assert.Equal(1, heap.ArrayCount);

        // Free it
        heap.Collect(memory);
        Assert.Equal(0, heap.ArrayCount);

        // Allocate new array - should reuse slot
        long handle2 = heap.Allocate(20);
        int idx2 = Value.GetHandle(handle2);

        // Store in global to keep it alive
        memory.SetGlobal(0, handle2);

        // Assert: New array should be in same or different slot, but should work
        Assert.Equal(1, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(idx2));
        Assert.Equal(20, heap.GetArrayLength(idx2));
    }

    [Fact]
    public void DeepNestedArraysArePreserved()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create chain of arrays: array1 -> array2 -> array3
        long array3 = heap.Allocate(1);
        long array2 = heap.Allocate(1);
        long array1 = heap.Allocate(1);

        int idx1 = Value.GetHandle(array1);
        int idx2 = Value.GetHandle(array2);
        int idx3 = Value.GetHandle(array3);

        heap.SetElement(idx1, 0, array2);
        heap.SetElement(idx2, 0, array3);

        // Store root in global
        memory.SetGlobal(0, array1);

        // Verify all arrays exist
        Assert.Equal(3, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: All arrays should still exist
        Assert.Equal(3, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(idx1));
        Assert.NotNull(heap.GetArray(idx2));
        Assert.NotNull(heap.GetArray(idx3));
        Assert.Equal(array2, heap.GetElement(idx1, 0));
        Assert.Equal(array3, heap.GetElement(idx2, 0));
    }

    [Fact]
    public void ArrayLostAfterFunctionReturnIsCollected()
    {
        // Arrange
        MemoryManager memory = new(0);
        ManagedHeap heap = new();

        // Act: Create frame, allocate array in local, then pop frame
        memory.CallStack.PushFrame(0, -1, 1);
        long taggedHandle = heap.Allocate(5);
        int handle = Value.GetHandle(taggedHandle);
        memory.SetLocal(0, taggedHandle);

        // Pop frame - array should become unreachable
        memory.CallStack.PopFrame();

        // Verify array exists before GC
        Assert.Equal(1, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Array should be collected
        Assert.Equal(0, heap.ArrayCount);
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(handle));
    }

    [Fact]
    public void ArrayInGlobalSurvivesFunctionReturn()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Store array in global, create and pop frame
        long taggedHandle = heap.Allocate(5);
        int handle = Value.GetHandle(taggedHandle);
        memory.SetGlobal(0, taggedHandle);

        memory.CallStack.PushFrame(0, -1, 0);
        memory.CallStack.PopFrame();

        // Verify array exists
        Assert.Equal(1, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Array should still exist
        Assert.Equal(1, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(handle));
        Assert.Equal(taggedHandle, memory.GetGlobal(0));
    }

    [Fact]
    public void ClearRemovesAllArrays()
    {
        // Arrange
        MemoryManager memory = new(2);
        ManagedHeap heap = new();

        // Act: Create multiple arrays
        long handle1 = heap.Allocate(1);
        long handle2 = heap.Allocate(2);
        memory.SetGlobal(0, handle1);
        memory.SetGlobal(1, handle2);

        Assert.Equal(2, heap.ArrayCount);

        // Clear
        heap.Clear();

        // Assert: All arrays should be removed
        Assert.Equal(0, heap.ArrayCount);
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(Value.GetHandle(handle1)));
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(Value.GetHandle(handle2)));
    }

    [Fact]
    public void ComplexScenarioWithMixedRoots()
    {
        // Arrange
        MemoryManager memory = new(2);
        ManagedHeap heap = new();

        // Act: Create complex scenario
        // - 2 arrays in globals (live)
        // - 1 array in local (live)
        // - 1 array on stack (live)
        // - 2 unreachable arrays (dead)
        // - 1 nested array (live)
        long global1 = heap.Allocate(1);
        long global2 = heap.Allocate(1);
        long local1 = heap.Allocate(1);
        long stack1 = heap.Allocate(1);
        long dead1 = heap.Allocate(1);
        long dead2 = heap.Allocate(1);
        long nested = heap.Allocate(1);

        // Setup roots
        memory.SetGlobal(0, global1);
        memory.SetGlobal(1, global2);
        memory.CallStack.PushFrame(0, -1, 1);
        memory.SetLocal(0, local1);
        memory.OperandStack.Push(stack1);

        // Nested array in global1
        heap.SetElement(Value.GetHandle(global1), 0, nested);

        // Verify all arrays exist
        Assert.Equal(7, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Only live arrays should exist (5: global1, global2, local1, stack1, nested)
        Assert.Equal(5, heap.ArrayCount);
        Assert.NotNull(heap.GetArray(Value.GetHandle(global1)));
        Assert.NotNull(heap.GetArray(Value.GetHandle(global2)));
        Assert.NotNull(heap.GetArray(Value.GetHandle(local1)));
        Assert.NotNull(heap.GetArray(Value.GetHandle(stack1)));
        Assert.NotNull(heap.GetArray(Value.GetHandle(nested)));
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(Value.GetHandle(dead1)));
        Assert.Throws<IndexOutOfRangeException>(() => heap.GetArray(Value.GetHandle(dead2)));
    }

    [Fact]
    public void ArrayWithMixedContentPreserved()
    {
        // Arrange
        MemoryManager memory = new(1);
        ManagedHeap heap = new();

        // Act: Create array with both numbers and array handles
        long childArray = heap.Allocate(2);
        long parentArray = heap.Allocate(5);
        int parentIdx = Value.GetHandle(parentArray);

        // Mix numbers and handles
        heap.SetElement(parentIdx, 0, 42);
        heap.SetElement(parentIdx, 1, childArray);
        heap.SetElement(parentIdx, 2, 100);
        heap.SetElement(parentIdx, 3, childArray);
        heap.SetElement(parentIdx, 4, 200);

        // Store parent in global
        memory.SetGlobal(0, parentArray);

        // Verify both arrays exist
        Assert.Equal(2, heap.ArrayCount);

        // Run GC
        heap.Collect(memory);

        // Assert: Both arrays should exist, and handles should be preserved
        Assert.Equal(2, heap.ArrayCount);
        Assert.Equal(42, heap.GetElement(parentIdx, 0));
        Assert.Equal(childArray, heap.GetElement(parentIdx, 1));
        Assert.Equal(100, heap.GetElement(parentIdx, 2));
        Assert.Equal(childArray, heap.GetElement(parentIdx, 3));
        Assert.Equal(200, heap.GetElement(parentIdx, 4));
    }
}
