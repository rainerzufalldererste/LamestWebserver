using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public abstract class ISessionIdentificator
    {
    }

    /// <summary>
    /// contains all available scopes for variables
    /// </summary>
    public enum EVariableScope : byte
    {
        /// <summary>
        /// available for all visitors of this page
        /// </summary>
        File,
        /// <summary>
        /// Available globally for this USER
        /// </summary>
        User,
        /// <summary>
        /// Available for the current User on only this page
        /// </summary>
        FileAndUser,
        /// <summary>
        /// Available for all Users on any page
        /// </summary>
        Global
    }

}
