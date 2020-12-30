using System;

namespace Ares {
	//Global enums only; specific enums are kept inside their respective classes
	public enum RoundMoment {StartOfRound, EndOfRound, StartOfTurn, EndOfTurn}
	public enum DoubleSetStageAction {Ignore, IncreaseByOne, ModifyByValue, SetToValue}
	public enum DoubleSetDurationAction {Ignore, IncreaseByOne, ResetToStart}
	public enum PowerScaling {Linear, ExponentialSimple, ExponentialSimpleSymmetric, ExponentialComplex, ExponentialComplexSymmetric, Custom}
}