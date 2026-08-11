![Logo](https://github.com/fnordstrom/EpidEvaluation/blob/master/EpidEvaluation.png)

# Evaluation of integrated images for static fields

## Description
Fast evaluation of EPID images for static fields at a single user-selectable point.

## Calculation
The predicted value (CU) is calculated using a factor-based method:

$$
P = MU \times EDWF(y) \times Intensity(x,y) \times OF(X,Y)
$$

The output factor (OF) is calculated as the mean of the output factors for two field sizes:

1. Based on the jaw settings
2. Based on the aperture

The equivalent depth-dependent wedge factor (EDWF) is calculated using the method proposed by Kupermann (2005) and the Golden STT tables.

Intensity and output factors are determined from reference beam data tables.

## Disclamer

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## License

Distributed under MIT License. Se `LICENSE.txt` for more information.
