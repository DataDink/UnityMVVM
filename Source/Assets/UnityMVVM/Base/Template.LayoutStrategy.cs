using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityMVVM.Base
{
  public abstract partial class Template
  {
    /// <summary>
    /// Defines a layout strategy for a configuration of <see cref="Template" /> <see cref="Instance" />s.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyLayoutStrategy : LayoutStrategy
    /// {
    ///   public void Arrange(IEnumerable&lt;Instance&gt; instances) 
    ///   {
    ///     foreach (var instance in instances) { instance.transform.position = ...; }
    ///   }
    /// }
    /// </code>
    /// </example>
    public abstract class LayoutStrategy : ScriptableObject 
    { 
      private static LayoutStrategy _default;
      /// <summary>
      /// The default <see cref="LayoutStrategy" />.
      /// </summary>
      public static LayoutStrategy Default => _default ??= CreateInstance<RadialLayoutStrategy>();
      /// <summary>
      /// Configures the layout of <see cref="Template" /> <see cref="Instance" />s.
      /// </summary>
      /// <param name="instances">The <see cref="Template" /> <see cref="Instance" />s</param>
      /// <example>
      /// <code>
      /// public class MyLayoutStrategy : LayoutStrategy
      /// {
      ///   public void Arrange(IEnumerable&lt;Instance&gt; instances) 
      ///   {
      ///     foreach (var instance in instances) { instance.transform.position = ...; }
      ///   }
      /// }
      /// </code>
      /// </example>
      public abstract void Arrange(object model, IEnumerable<Instance> instances);
      /// <summary>
      /// Configures an infinite layout pattern.
      /// </summary>
      public abstract class PatternStrategy : LayoutStrategy
      {
        /// <summary>
        /// Measures the total <see cref="Bounds" /> of an <see cref="Instance" /> based on associated <see cref="Renderer" />s.
        /// </summary>
        /// <param name="prefab">The <see cref="Instance" /> to measure.</param>
        /// <returns>The calculated <see cref="Bounds" /> of the <see cref="Instance" />.</returns>
        /// <example>
        /// <code>
        /// var scale = instances.Select(i => Measure(i).size.magnitude).DefaultIfEmpty().Max();
        /// </code>
        /// </example> 
        public static Bounds Measure(Instance prefab) => prefab.GetComponentsInChildren<Renderer>().Aggregate(new Bounds(prefab.transform.position, Vector3.zero), (a,r) => { a.Encapsulate(r.bounds); return a; });
        /// <summary>
        /// Specifies the starting position.
        /// </summary>
        [SerializeField] public Selector Skip = "0";
        /// <summary>
        /// Specifies the increment of positions.
        /// </summary>
        [SerializeField] public Selector Step = "1";
        /// <summary>
        /// Specifies the number of positions.
        /// </summary>
        [SerializeField] public Selector Take = "0";
        /// <summary>
        /// Specifies the direction of positions.
        /// </summary>
        [SerializeField] public Selector Rotate = "0,0,0";
        /// <inheritdoc />
        public override void Arrange(object model, IEnumerable<Instance> instances)
        {
          var scale = instances.Select(i => Measure(i).size.magnitude).DefaultIfEmpty().Max();
          var rotate = Selector.Converters.Vector3Converter.CanParse(Rotate) 
            ? Selector.Converters.Vector3Converter.Parse(Rotate) 
            : Rotate.Select<Vector3>(model);
          var rotation = Quaternion.Euler(rotate);
          var skip = Selector.Converters.Int32Converter.CanParse(Skip)
            ? Selector.Converters.Int32Converter.Parse(Skip)
            : Skip.Select<int>(model);
          var skipped = Pattern().Skip((int)skip);
          var step = Selector.Converters.Int32Converter.CanParse(Step)
            ? Selector.Converters.Int32Converter.Parse(Step)
            : Step.Select<int>(model);
          var stepped = step > 0 ? skipped.Where((_,i) => i % step == 0) : skipped;
          var take =  Selector.Converters.Int32Converter.CanParse(Take)
            ? Selector.Converters.Int32Converter.Parse(Take)
            : Take.Select<int>(model);
          var taken = take > 0 ? stepped.Take((int)take) : stepped;
          var positions = taken.GetEnumerator();
          foreach (var item in instances) { item.transform.localPosition = positions.MoveNext() ? rotation*positions.Current*scale : default; }
        }
        /// <summary>
        /// The enumeration of positions for this strategy
        /// </summary>
        protected abstract IEnumerable<Vector3> Pattern();
      }
      /// <summary>
      /// Configures an aligned layout pattern.
      /// </summary>
      /// <remarks>
      /// <para> Pattern: 0, -1, 1, -2, 2, -3, 3, ...</para>
      /// </remarks>
      /// <example>
      /// <para>Note: A <see cref="Template.LayoutStrategy" /> is typically configured in the Unity Inspector.</para>
      /// <code>
      /// var template = GetComponent&lt;Template&gt;();
      /// template.Layout = ScriptableObject.CreateInstance&lt;AlignedLayoutStrategy&gt;();
      /// </code>
      /// </example>
      [CreateAssetMenu(fileName = "AlignedLayout", menuName = "Bindings/Layouts/Aligned")]
      public class AlignedLayoutStrategy : PatternStrategy
      {
        /// <inheritdoc/>
        protected override IEnumerable<Vector3> Pattern() => Enumerate();
        /// <summary>
        /// An infinite enumeration of <see cref="Vector3" />s.
        /// </summary>
        public static IEnumerable<Vector3> Enumerate()
        {
          yield return Vector3.zero;
          var extent = 1f;
          while(true) { 
            yield return Vector3.right*-extent;
            yield return Vector3.right*extent++;
          }
        }
      }
      /// <summary>
      /// Configures an expanding square pattern.
      /// </summary>
      /// <remarks>
      /// <para> Pattern: (0,0), (-1,-1), (0,-1), (1,-1), (1,0), (1,1), ...</para>
      /// </remarks>
      /// <example>
      /// <para>Note: A <see cref="Template.LayoutStrategy" /> is typically configured in the Unity Inspector.</para>
      /// <code>
      /// var template = GetComponent&lt;Template&gt;();
      /// template.Layout = ScriptableObject.CreateInstance&lt;SquareLayoutStrategy&gt;();
      /// </code>
      /// </example>
      [CreateAssetMenu(fileName = "SquareLayout", menuName = "Bindings/Layouts/Square")]
      public class SquareLayoutStrategy : PatternStrategy
      {
        /// <inheritdoc/>
        protected override IEnumerable<Vector3> Pattern() => Enumerate();
        /// <summary>
        /// An infinite enumeration of <see cref="Vector3" />s.
        /// </summary>
        public static IEnumerable<Vector3> Enumerate()
        {
          yield return Vector3.zero;
          var range = 1f;
          while (true) {
            var col = -range;
            var row = -range;
            while (col < range) { yield return col++*Vector3.right+row*Vector3.back; }
            while (row < range) { yield return col*Vector3.right+row++*Vector3.back; }
            while (col > -range) { yield return col--*Vector3.right+row*Vector3.back; }
            while (row > -range) { yield return col*Vector3.right+row--*Vector3.back; }
            range++;
          }
        }
      }
      /// <summary>
      /// Configures an expanding radial pattern.
      /// </summary>
      /// <remarks>
      /// <para> Pattern: 0, 45, 90, 135, ...</para>
      /// </remarks>
      /// <example>
      /// <para>Note: A <see cref="Template.LayoutStrategy" /> is typically configured in the Unity Inspector.</para>
      /// <code>
      /// var template = GetComponent&lt;Template&gt;();
      /// template.Layout = ScriptableObject.CreateInstance&lt;RadialLayoutStrategy&gt;();
      /// </code>
      /// </example>
      [CreateAssetMenu(fileName = "RadialLayout", menuName = "Bindings/Layouts/Radial")]
      public class RadialLayoutStrategy : PatternStrategy
      {
        /// <inheritdoc/>
        protected override IEnumerable<Vector3> Pattern() => Enumerate();
        /// <summary>
        /// An infinite enumeration of <see cref="Vector3" />s.
        /// </summary>
        public static IEnumerable<Vector3> Enumerate()
        {
          yield return Vector3.zero;
          var range = 1f;
          while (true) {
            var pitch = Mathf.Pow(2, range+2);
            for (var r = 0; r < pitch; r++) {
              var angle = 2*Mathf.PI*r/pitch;
              yield return Mathf.Cos(angle)*range*Vector3.forward+Mathf.Sin(angle)*range*Vector3.right;
            }
            range++;
          }
        }
      }
    }
  }
}